# MiUni

Backend .NET y frontend React. El chat consume el servicio Python `rag-miuni`.

## Requisitos

- [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) abierto, con WSL 2 y contenedores Linux.
- .NET SDK 10 y `dotnet-ef` 10.0.12.
- Node compatible con Vite (22.12 o superior).

Si falta WSL, ejecutar `wsl --install` como administrador y reiniciar. Si usas un
PostgreSQL externo con las extensiones del proyecto, no necesitas Docker local.

## Base de datos

Desde la raíz del repo, en PowerShell:

```powershell
powershell -NoProfile -File .\scripts\dev.ps1 -Action init
docker compose config --quiet
docker compose up -d --build --wait db
```

`init` se ejecuta solo la primera vez: crea `.env` con claves aleatorias. Si ya tienes
`.env`, conservarlo y usar `-Action check`. No subirlo a Git; `.env.example` es la plantilla.
Con un volumen existente, conservar su contraseña: editar `.env` no la cambia en PostgreSQL.

Aplicar las migraciones:

```powershell
# Solo si no tienes EF CLI instalado:
dotnet tool install --global dotnet-ef --version 10.0.12

powershell -NoProfile -File .\scripts\dev.ps1 -Action migrate
```

Si ya tienes EF CLI de otra versión, actualizarlo con `dotnet tool update --global dotnet-ef --version 10.0.12`.
La base queda en `127.0.0.1:5433` por defecto, con PostgreSQL 16, pgvector, PostGIS y pgRouting.
Las migraciones crean las tablas e índices RAG; los documentos y embeddings se cargan aparte desde `rag-miuni`.

## Backend y frontend

Configurar en `.env` la conexión PostgreSQL, `JWT_KEY` y
`RAG_BASE_URL=http://127.0.0.1:8000/api/v1/`.

```powershell
powershell -NoProfile -File .\scripts\dev.ps1 -Action api
```

El launcher carga `.env` y arranca el backend en `http://localhost:5124`.
En otra terminal:

```powershell
cd MiUni.Web
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
npm ci
npm run dev
```

Dejar `VITE_API_URL=` vacío para usar el proxy del frontend. Para probar el chat,
iniciar también `rag-miuni` con su configuración e índice. Compose levanta únicamente
PostgreSQL; Python, backend y frontend se ejecutan por separado.

## Uso diario

```powershell
docker compose up -d --wait db
docker compose ps
docker compose logs --tail 40 db
docker compose stop db
```

Los datos persisten en el volumen. `docker compose down -v` lo elimina.
