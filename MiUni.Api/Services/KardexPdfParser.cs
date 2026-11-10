using System.Globalization;
using System.Text.RegularExpressions;
using MiUni.Api.Enums;
using UglyToad.PdfPig;

namespace MiUni.Api.Services;

public record MateriaImportada(string Materia, string? Clave, string? Periodo, short? Semestre,
    decimal? Calificacion, short Creditos, EstadoMateria Estado);

// Lee el kárdex del portal de alumnos de la UNISON y otros kárdex en tabla: una fila por
// materia con columnas de clave, materia, créditos, calificación y periodo o semestre.
// El periodo y el semestre también pueden venir como títulos de sección ("CICLO 2025-1",
// "3er SEMESTRE") encima de cada tabla.
//
// Igual que con el horario, las columnas se deducen de los huecos verticales entre las
// palabras y se nombran con el encabezado más cercano.
public static partial class KardexPdfParser
{
    private enum Tipo { Otro, Clave, Materia, Creditos, Calificacion, Regularizacion, Periodo, Semestre, Estado, Bajas }

    private static readonly Dictionary<string, Tipo> Encabezados = new()
    {
        ["CLAVE"] = Tipo.Clave, ["CVE"] = Tipo.Clave,
        ["MATERIA"] = Tipo.Materia, ["MATERIAS"] = Tipo.Materia, ["ASIGNATURA"] = Tipo.Materia, ["DESCRIPCION"] = Tipo.Materia, ["NOMBRE"] = Tipo.Materia,
        ["CREDITOS"] = Tipo.Creditos, ["CRED"] = Tipo.Creditos, ["CR"] = Tipo.Creditos, ["CREDITO"] = Tipo.Creditos,
        ["CALIFICACION"] = Tipo.Calificacion, ["CALIF"] = Tipo.Calificacion, ["CAL"] = Tipo.Calificacion,
        ["CALIFICACIONES"] = Tipo.Calificacion, ["ORD"] = Tipo.Calificacion, ["REG"] = Tipo.Regularizacion,
        ["CIC"] = Tipo.Periodo, ["B"] = Tipo.Bajas,
        ["PERIODO"] = Tipo.Periodo, ["CICLO"] = Tipo.Periodo,
        ["SEMESTRE"] = Tipo.Semestre, ["SEM"] = Tipo.Semestre,
        ["ESTADO"] = Tipo.Estado, ["SITUACION"] = Tipo.Estado, ["TIPO"] = Tipo.Estado, ["OBSERVACIONES"] = Tipo.Estado,
    };

    private static readonly HashSet<string> Acreditada = ["AC", "ACR", "ACRED", "ACREDITADA", "ACREDITADO", "A", "APROBADA", "APR"];
    private static readonly HashSet<string> NoAcreditada = ["NA", "NP", "NOACREDITADA", "REPROBADA", "REP", "R", "SD"];
    private static readonly HashSet<string> Baja = ["BAJA", "BA", "B", "BJ"];

    private sealed record Columna(double Inicio, double Fin)
    {
        public Tipo Tipo { get; set; }
    }

    [GeneratedRegex(@"\b(20\d{2})\s*[-/]\s*([1-3])\b")]
    private static partial Regex RegexPeriodo();

    [GeneratedRegex(@"\b(\d{1,2})\s*(?:°|º|o|er|do|ro|to|vo|no|mo)?\.?\s*SEMESTRE\b|\bSEMESTRE\s*(\d{1,2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex RegexSemestre();

    [GeneratedRegex(@"^2(\d{2})([1-3])$")]
    private static partial Regex RegexCiclo();

    [GeneratedRegex(@"^\d{3,6}[A-Z]?$")]
    private static partial Regex RegexClave();

    // En el kárdex de la UNISON "PAR. ACRED" queda 2 pt más arriba que el resto de su fila.
    private const double ToleranciaLinea = 3;

    public static IReadOnlyList<MateriaImportada> Leer(Stream pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        var palabras = documento.GetPages().SelectMany(p => p.GetWords().Select(w => new PalabraPdf(
            w.Text, w.BoundingBox.Left, w.BoundingBox.Width, w.BoundingBox.Bottom, p.Number))).ToList();
        return Leer(palabras);
    }

    public static IReadOnlyList<MateriaImportada> Leer(IEnumerable<PalabraPdf> palabras)
    {
        var materias = new List<MateriaImportada>();
        string? periodo = null;
        short? semestre = null;
        foreach (var pagina in palabras.GroupBy(p => p.Pagina).OrderBy(g => g.Key))
            materias.AddRange(LeerPagina(HorarioPdfParser.AgruparLineas(pagina, ToleranciaLinea), ref periodo, ref semestre));

        // Sin columna ni títulos de semestre, se deduce del orden de los periodos:
        // el primer periodo cursado es el 1er semestre, el segundo el 2°, etc.
        if (materias.All(m => m.Semestre is null))
        {
            var periodos = materias.Select(m => m.Periodo).OfType<string>().Distinct().Order(StringComparer.Ordinal).ToList();
            if (periodos.Count is > 0 and <= 12)
                materias = materias.Select(m => m.Periodo is null ? m
                    : m with { Semestre = (short)(periodos.IndexOf(m.Periodo) + 1) }).ToList();
        }
        return materias;
    }

    private static bool EsEncabezado(List<PalabraPdf> linea)
    {
        var tipos = linea.Select(TipoEncabezado).ToHashSet();
        return tipos.Contains(Tipo.Materia) && (tipos.Contains(Tipo.Calificacion) || tipos.Contains(Tipo.Creditos));
    }

    // Las rayas que rodean "MATERIAS" o "CREDITOS" en el pie no hacen encabezado.
    private static Tipo TipoEncabezado(PalabraPdf palabra) => palabra.Texto.Contains('-')
        ? Tipo.Otro : Encabezados.GetValueOrDefault(HorarioPdfParser.Normalizar(palabra.Texto));

    private static bool EsRaya(List<PalabraPdf> linea) => linea.All(p => p.Texto.All(c => c is '-' or '_' or '='));

    private static List<MateriaImportada> LeerPagina(List<List<PalabraPdf>> lineas, ref string? periodo, ref short? semestre)
    {
        var resultado = new List<MateriaImportada>();
        List<PalabraPdf>? encabezado = null;
        var bloque = new List<List<PalabraPdf>>();

        void Cerrar(ref string? periodo, ref short? semestre)
        {
            if (encabezado is not null && bloque.Count > 0)
                resultado.AddRange(LeerTabla(encabezado, bloque, ref periodo, ref semestre));
            bloque.Clear();
        }

        foreach (var linea in lineas)
        {
            if (EsEncabezado(linea))
            {
                Cerrar(ref periodo, ref semestre);
                encabezado = linea;
            }
            else if (EsRaya(linea))
            {
                // Una raya después de los datos cierra la tabla (lo que sigue es el pie).
                if (bloque.Count > 0) { Cerrar(ref periodo, ref semestre); encabezado = null; }
            }
            else if (encabezado is null) LeerTitulo(linea, ref periodo, ref semestre);
            else bloque.Add(linea);
        }
        Cerrar(ref periodo, ref semestre);
        return resultado;
    }

    // Títulos de sección como "CICLO 2025-1" o "3er SEMESTRE": un periodo o semestre sin más números.
    private static bool EsTitulo(List<PalabraPdf> linea)
    {
        if (linea.Count > 6) return false;
        var texto = string.Join(' ', linea.Select(p => p.Texto));
        if (!RegexPeriodo().IsMatch(texto) && !RegexSemestre().IsMatch(texto)) return false;
        var resto = RegexSemestre().Replace(RegexPeriodo().Replace(texto, " "), " ");
        return !resto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(t => t.Any(char.IsDigit));
    }

    private static bool LeerTitulo(List<PalabraPdf> linea, ref string? periodo, ref short? semestre)
    {
        if (!EsTitulo(linea)) return false;
        var texto = string.Join(' ', linea.Select(p => p.Texto));
        var mp = RegexPeriodo().Match(texto);
        if (mp.Success) periodo = $"{mp.Groups[1].Value}-{mp.Groups[2].Value}";
        var ms = RegexSemestre().Match(texto);
        if (ms.Success && short.TryParse(ms.Groups[1].Success ? ms.Groups[1].Value : ms.Groups[2].Value, out var s) && s is >= 1 and <= 12)
            semestre = s;
        return true;
    }

    private static IEnumerable<MateriaImportada> LeerTabla(List<PalabraPdf> encabezado, List<List<PalabraPdf>> lineas,
        ref string? periodo, ref short? semestre)
    {
        // Un salto vertical grande separa tablas (o la tabla de las notas al pie): cada tramo
        // calcula sus columnas, pero todos se nombran con el mismo encabezado.
        var resultado = new List<MateriaImportada>();
        var tramo = new List<List<PalabraPdf>>();
        var anteriorY = encabezado[0].Y;
        foreach (var linea in lineas)
        {
            if (anteriorY - linea[0].Y > 25 && tramo.Count > 0)
            {
                resultado.AddRange(LeerTramo(encabezado, tramo, ref periodo, ref semestre));
                tramo = [];
            }
            tramo.Add(linea);
            anteriorY = linea[0].Y;
        }
        if (tramo.Count > 0) resultado.AddRange(LeerTramo(encabezado, tramo, ref periodo, ref semestre));
        return resultado;
    }

    private static List<MateriaImportada> LeerTramo(List<PalabraPdf> encabezado, List<List<PalabraPdf>> tabla,
        ref string? periodo, ref short? semestre)
    {
        // Títulos y párrafos largos no definen columnas.
        var columnas = Columnas(tabla.Where(l => !EsTitulo(l) && l.Count <= 14).Append(encabezado).ToList());
        Nombrar(columnas, encabezado);
        var resultado = new List<MateriaImportada>();
        if (!columnas.Any(c => c.Tipo == Tipo.Materia)) return resultado;
        var tieneClave = columnas.Any(c => c.Tipo == Tipo.Clave);

        Dictionary<Tipo, string>? fila = null;
        string? periodoFila = periodo;
        short? semestreFila = semestre;
        double anteriorY = tabla[0][0].Y;

        void Emitir()
        {
            if (fila is not null && Materia(fila, periodoFila, semestreFila, tieneClave) is { } m) resultado.Add(m);
            fila = null;
        }

        foreach (var linea in tabla)
        {
            if (EsTitulo(linea))
            {
                Emitir();
                LeerTitulo(linea, ref periodo, ref semestre);
                (periodoFila, semestreFila, anteriorY) = (periodo, semestre, linea[0].Y);
                continue;
            }
            var celdas = Celdas(linea, columnas);
            var esDato = celdas.ContainsKey(Tipo.Calificacion) || celdas.ContainsKey(Tipo.Creditos)
                || celdas.TryGetValue(Tipo.Clave, out var clave) && RegexClave().IsMatch(clave);
            var soloMateria = celdas.Keys.All(t => t == Tipo.Materia);
            var lejos = anteriorY - linea[0].Y > 14;
            anteriorY = linea[0].Y;

            if (!esDato && (!soloMateria || lejos || fila is null))
            {
                // Texto fuera de la tabla, como notas al pie.
                Emitir();
                continue;
            }

            if (esDato && fila is not null && (fila.ContainsKey(Tipo.Calificacion) || fila.ContainsKey(Tipo.Creditos)
                || fila.ContainsKey(Tipo.Clave)))
                Emitir();

            if (fila is null) { fila = celdas; continue; }
            // Nombre de materia partido en varias líneas.
            foreach (var (tipo, texto) in celdas)
                fila[tipo] = fila.TryGetValue(tipo, out var previo) ? $"{previo} {texto}" : texto;
        }
        Emitir();
        return resultado;
    }

    private static MateriaImportada? Materia(Dictionary<Tipo, string> fila, string? periodo, short? semestre, bool tieneClave)
    {
        var nombre = Limpiar(fila.GetValueOrDefault(Tipo.Materia));
        if (nombre is null || !nombre.Any(char.IsLetter)) return null;
        // Si la tabla trae clave, una fila sin clave válida es texto suelto (una nota al pie).
        var clave = Limpiar(fila.GetValueOrDefault(Tipo.Clave));
        if (tieneClave && (clave is null || !RegexClave().IsMatch(clave))) return null;

        // La regularización, si la hay, sustituye a la ordinaria.
        var textoCalif = Numero(fila.GetValueOrDefault(Tipo.Regularizacion)) is not null
            ? Limpiar(fila.GetValueOrDefault(Tipo.Regularizacion))
            : Limpiar(fila.GetValueOrDefault(Tipo.Calificacion));
        var textoEstado = Limpiar(fila.GetValueOrDefault(Tipo.Estado));
        decimal? calificacion = null;
        EstadoMateria estado = EstadoMateria.Cursando;
        if (textoCalif is not null && decimal.TryParse(textoCalif.Split(' ')[0].Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) && valor is >= 0 and <= 100)
        {
            calificacion = valor;
            estado = valor >= 60 ? EstadoMateria.Aprobada : EstadoMateria.Reprobada;
        }
        else if (Clasificar(textoCalif) is { } porTexto) estado = porTexto;
        if (Clasificar(textoEstado) == EstadoMateria.Baja
            || calificacion is null && estado == EstadoMateria.Cursando && Numero(fila.GetValueOrDefault(Tipo.Bajas)) > 0)
            estado = EstadoMateria.Baja;

        short creditos = 0;
        if (Limpiar(fila.GetValueOrDefault(Tipo.Creditos)) is { } textoCred
            && short.TryParse(textoCred.Split(' ')[0], out var cred) && cred is >= 0 and <= 30)
            creditos = cred;

        var periodoFila = Periodo(fila.GetValueOrDefault(Tipo.Periodo)) ?? periodo;
        var semestreFila = Limpiar(fila.GetValueOrDefault(Tipo.Semestre)) is { } ts
            && short.TryParse(new string(ts.TakeWhile(char.IsDigit).ToArray()), out var s) && s is >= 1 and <= 12 ? s : semestre;

        return new MateriaImportada(Recortar(nombre, 120)!, Recortar(clave, 20), periodoFila, semestreFila,
            calificacion, creditos, estado);
    }

    // "2025-1", "2025/1" o el ciclo compacto de la UNISON: "2251" = 2025-1, "2232" = 2023-2.
    private static string? Periodo(string? texto)
    {
        texto = Limpiar(texto);
        if (texto is null) return null;
        if (RegexPeriodo().Match(texto) is { Success: true } m) return $"{m.Groups[1].Value}-{m.Groups[2].Value}";
        return RegexCiclo().Match(texto) is { Success: true } c ? $"20{c.Groups[1].Value}-{c.Groups[2].Value}" : null;
    }

    private static decimal? Numero(string? texto) =>
        Limpiar(texto) is { } t && decimal.TryParse(t.Split(' ')[0].TrimStart('*').Replace(',', '.'),
            NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;

    private static EstadoMateria? Clasificar(string? texto)
    {
        if (texto is null) return null;
        var t = HorarioPdfParser.Normalizar(texto);
        if (Baja.Contains(t) || t.Contains("BAJA")) return EstadoMateria.Baja;
        if (NoAcreditada.Contains(t) || t.Contains("REPROB") || t.Contains("NOACRED")) return EstadoMateria.Reprobada;
        if (Acreditada.Contains(t) || t.Contains("APROB") || t.Contains("ACRED")) return EstadoMateria.Aprobada;
        if (t.Contains("CURS") || t.Contains("INSCRIT")) return EstadoMateria.Cursando;
        return null;
    }

    // Columnas por proyección: una x es hueco entre columnas si casi ninguna línea la cubre.
    // Así una línea suelta que cruza columnas (una nota, "ACREDITACIÓN DE INGLÉS...") no las une.
    private static List<Columna> Columnas(List<List<PalabraPdf>> lineas)
    {
        var palabras = lineas.SelectMany(l => l).Where(p => p.Ancho < 250).ToList();
        if (palabras.Count == 0) return [];
        var inicio = (int)Math.Floor(palabras.Min(p => p.X));
        var cobertura = new int[(int)Math.Ceiling(palabras.Max(p => p.Fin)) - inicio + 2];
        foreach (var linea in lineas)
        {
            var cubre = new bool[cobertura.Length];
            foreach (var p in linea.Where(p => p.Ancho < 250))
                for (var x = (int)Math.Floor(p.X) - inicio; x <= (int)Math.Ceiling(p.Fin) - inicio && x < cubre.Length; x++)
                    cubre[x] = true;
            for (var x = 0; x < cubre.Length; x++) if (cubre[x]) cobertura[x]++;
        }

        var umbral = lineas.Count / 8;
        var columnas = new List<Columna>();
        int? desde = null;
        var vacio = 0;
        for (var x = 0; x < cobertura.Length; x++)
        {
            if (cobertura[x] > umbral)
            {
                if (desde is null) desde = x;
                else if (vacio >= 4) { columnas.Add(new Columna(desde.Value + inicio, x - vacio + inicio)); desde = x; }
                vacio = 0;
            }
            else if (desde is not null) vacio++;
        }
        if (desde is not null) columnas.Add(new Columna(desde.Value + inicio, cobertura.Length - vacio + inicio));
        return columnas;
    }

    // Cada encabezado nombra la columna más cercana, empezando por los que quedan encima de
    // una; un encabezado sin datos debajo (p. ej. REG vacía) no le roba la columna al vecino.
    private static void Nombrar(List<Columna> columnas, List<PalabraPdf> encabezado)
    {
        if (columnas.Count == 0) return;
        var candidatos = encabezado
            .Select(p => (Tipo: TipoEncabezado(p), p.Centro))
            .Where(h => h.Tipo != Tipo.Otro)
            .Select(h => (h.Tipo, Columna: columnas.MinBy(c => Distancia(c, h.Centro))!, h.Centro))
            .OrderBy(h => Distancia(h.Columna, h.Centro));
        foreach (var (tipo, columna, centro) in candidatos)
            if (columna.Tipo == Tipo.Otro && (Distancia(columna, centro) <= 15 || tipo == Tipo.Materia))
                columna.Tipo = tipo;
    }

    private static double Distancia(Columna c, double x) => x < c.Inicio ? c.Inicio - x : x > c.Fin ? x - c.Fin : 0;

    private static Dictionary<Tipo, string> Celdas(List<PalabraPdf> linea, List<Columna> columnas)
    {
        var celdas = new Dictionary<Tipo, string>();
        foreach (var palabra in linea)
        {
            var tipo = columnas.MinBy(c => Distancia(c, palabra.Centro))!.Tipo;
            celdas[tipo] = celdas.TryGetValue(tipo, out var texto) ? $"{texto} {palabra.Texto}" : palabra.Texto;
        }
        return celdas;
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 || limpio == "-" ? null : limpio;
    }

    private static string? Recortar(string? texto, int maximo) =>
        texto is null ? null : texto.Length <= maximo ? texto : texto[..maximo].TrimEnd();
}
