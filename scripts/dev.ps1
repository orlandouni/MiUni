param(
    [ValidateSet('init', 'check', 'migrate', 'api')]
    [string]$Action = 'check',
    [string]$EnvPath = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $EnvPath) { $EnvPath = Join-Path $projectRoot '.env' }

function New-RandomSecret {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

if ($Action -eq 'init') {
    if (Test-Path -LiteralPath $EnvPath) {
        throw 'El archivo .env ya existe; no se sobrescribe ni se cambia la clave de un volumen existente.'
    }
    $template = [IO.File]::ReadAllText((Join-Path $projectRoot '.env.example'))
    $template = $template.Replace('MIUNI_POSTGRES_PASSWORD=', "MIUNI_POSTGRES_PASSWORD='$(New-RandomSecret)'")
    $template = $template.Replace('JWT_KEY=', "JWT_KEY='$(New-RandomSecret)'")
    [IO.File]::WriteAllText($EnvPath, $template, (New-Object Text.UTF8Encoding($false)))
    Write-Output 'Se creo .env con claves aleatorias. No publiques este archivo.'
    return
}

if (-not (Test-Path -LiteralPath $EnvPath)) {
    throw 'Falta .env. Para una instalacion nueva: powershell -File .\scripts\dev.ps1 -Action init'
}

# Supported dotenv subset: one KEY=value per line, or single-quoted literal values.
# No evaluation or interpolation of expressions is performed.
$values = @{}
$lineNumber = 0
foreach ($line in [IO.File]::ReadAllLines($EnvPath)) {
    $lineNumber++
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed -notmatch '^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        throw "Formato .env invalido en linea $lineNumber. Usa CLAVE=valor."
    }
    $name = $Matches[1]
    $value = $Matches[2].Trim()
    if ($value.StartsWith("'")) {
        if ($value.Length -lt 2 -or -not $value.EndsWith("'")) {
            throw "Comillas sin cerrar en linea $lineNumber."
        }
        $value = $value.Substring(1, $value.Length - 2).Replace("\'", "'")
    } else {
        if ($value.Contains('$') -or $value.Contains('"') -or $value.Contains("'")) {
            throw "Usa comillas simples para valores especiales en linea $lineNumber."
        }
        $value = ($value -replace '\s+#.*$', '').TrimEnd()
    }
    $values[$name] = $value
}

foreach ($name in @('MIUNI_POSTGRES_HOST', 'MIUNI_POSTGRES_PORT', 'MIUNI_POSTGRES_DB',
                    'MIUNI_POSTGRES_USER', 'MIUNI_POSTGRES_PASSWORD', 'JWT_KEY', 'RAG_BASE_URL')) {
    # Match Compose precedence: explicit process variables override the .env file.
    $override = [Environment]::GetEnvironmentVariable($name)
    if ($null -ne $override) { $values[$name] = $override }
    if ([string]::IsNullOrWhiteSpace($values[$name])) { throw "Falta $name en .env o en el entorno." }
}
$dbPort = 0
if (-not [int]::TryParse($values['MIUNI_POSTGRES_PORT'], [ref]$dbPort) -or $dbPort -lt 1 -or $dbPort -gt 65535) {
    throw 'MIUNI_POSTGRES_PORT debe ser un puerto valido.'
}
if ($values['JWT_KEY'].Length -lt 32) { throw 'JWT_KEY debe tener al menos 32 caracteres.' }
$ragUri = $null
if (-not [Uri]::TryCreate($values['RAG_BASE_URL'], [UriKind]::Absolute, [ref]$ragUri) -or
    $ragUri.Scheme -notin @('http', 'https') -or -not $ragUri.AbsolutePath.EndsWith('/api/v1/')) {
    throw 'RAG_BASE_URL debe ser una URL http/https absoluta que termine en /api/v1/.'
}

# The builder quotes separators inside passwords, rather than concatenating secrets.
$connection = New-Object System.Data.Common.DbConnectionStringBuilder
$connection['Host'] = $values['MIUNI_POSTGRES_HOST']
$connection['Port'] = $dbPort
$connection['Database'] = $values['MIUNI_POSTGRES_DB']
$connection['Username'] = $values['MIUNI_POSTGRES_USER']
$connection['Password'] = $values['MIUNI_POSTGRES_PASSWORD']
$env:ConnectionStrings__MiUniDb = $connection.get_ConnectionString()
$env:Jwt__Key = $values['JWT_KEY']
$env:Rag__BaseUrl = $values['RAG_BASE_URL']
$env:ASPNETCORE_ENVIRONMENT = 'Docker'
$env:DOTNET_ENVIRONMENT = 'Docker'

if ($Action -eq 'check') {
    Write-Output 'Configuracion valida. Claves leidas de .env/entorno; no se muestran. No se ha probado la conexion.'
    return
}

Push-Location (Join-Path $projectRoot 'MiUni.Api')
try {
    if ($Action -eq 'migrate') {
        & dotnet ef database update
    } else {
        & dotnet run --no-launch-profile --urls http://localhost:5124
    }
    if ($LASTEXITCODE -ne 0) { throw "dotnet termino con codigo $LASTEXITCODE." }
} finally { Pop-Location }
