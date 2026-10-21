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

## Reportes y administración — RF-05d / RF-13

Implementados CRUD de reportes, listado administrativo con datos relacionados y
contadores, y creación/edición/baja de lugares y categorías restringidas a
Administrador. [Contratos, permisos y ejemplos](docs/administracion-api.md).
Peticiones listas para probar: `MiUni.Api/Administracion.http`.

## Login y permisos por roles

### Administrador compartido para desarrollo

El launcher `-Action api` usa el ambiente Docker y no crea la cuenta de prueba.
Para activarla en Development conservando la conexión y JWT de tu `.env`, ejecutar
desde la raíz del repo:

```powershell
& .\scripts\dev.ps1 -Action check
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
cd MiUni.Api
dotnet ef database update
dotnet run --no-launch-profile --urls http://localhost:5124
```

Los comandos `dotnet` de esta sección usan esa misma terminal y configuración.

Al arrancar en **Development**, el seed crea automáticamente esta cuenta local:

- Correo: `admin.dev@unison.mx`
- Contraseña de prueba: `MiUni.Dev2026!`
- Roles: `Estudiante` y `Administrador`

Todos usan las mismas credenciales de prueba en sus propias bases. El seed usa
Identity para generar el hash de contraseña y una transacción para crear cuenta
y permisos. Es repetible: no duplica cuentas ni restablece la contraseña o el
perfil en cada arranque. Las instancias concurrentes se coordinan con un bloqueo
transaccional PostgreSQL. No promueve otras cuentas si el correo ya está ocupado.

Los valores están versionados en `appsettings.json`, sección `DevelopmentAdmin`.
Para desactivarlo, establecer `DevelopmentAdmin__Enabled=false`. Puede configurarse
otro correo/contraseña **antes de la primera creación** usando variables de entorno
`DevelopmentAdmin__Email` y `DevelopmentAdmin__Password`. Si la cuenta ya existe,
cambiar estos valores no la modifica.

El seed nunca se ejecuta fuera de Development, aunque Enabled sea true. Estas
credenciales públicas son sólo de desarrollo; no reutilizar esa base como producción.
Para asignar administradores reales se conserva el comando local `--asignar-admin`.

Un solo login para todos. Los roles se almacenan en las tablas de ASP.NET Identity
(`AspNetRoles` y `AspNetUserRoles`), se incluyen en el JWT y se devuelven en
`roles` tanto en `/api/Auth/login` como en `/api/Auth/me`.

- `Estudiante`: usuario normal; consulta el campus y envía solicitudes de negocio.
- `Propietario`: también puede editar nombre y descripción de sus negocios aprobados.
- `Administrador`: revisa solicitudes y puede editar los negocios aprobados.

Los permisos son acumulables. El registro público asigna únicamente `Estudiante`;
no acepta roles ni identificadores de propietario enviados por el cliente. Se
conserva la restricción existente de correo `@unison.mx` (también para quien
solicite un negocio). La columna antigua `AspNetUsers.Rol` queda como dato legado;
los permisos se consultan exclusivamente en Identity. La migración conserva los
administradores legados y asigna el rol base a las cuentas existentes.

Aplicar la migración desde `MiUni.Api` antes de iniciar la versión nueva:

```powershell
dotnet ef database update
```

Para asignar el primer administrador, registrar primero su cuenta y ejecutar
localmente con el correo real (no hay endpoint público ni contraseña por defecto):

```powershell
dotnet run -- --asignar-admin "correo@unison.mx"
```

El comando termina después de asignar el permiso. Iniciar luego con `dotnet run`.
El login dirige al usuario al mapa, al propietario a `/negocios` y al administrador
a `/admin`. Desde el mapa, cualquier cuenta autenticada puede entrar a
**Mis negocios / Registrar negocio**. Las rutas protegidas consultan `/api/Auth/me`.

Flujo de negocios:

1. `POST /api/Negocio/solicitudes`: nombre, descripción, categoría y coordenadas;
   queda pendiente y oculto del mapa. El propietario se toma de la sesión.
2. `GET /api/Negocio/mios`: solicitudes y negocios propios.
3. `GET /api/Negocio/solicitudes`: pendientes, sólo administradores.
4. `POST /api/Negocio/solicitudes/{id}/aprobar` o `/rechazar`: sólo administradores.
   Aprobar publica el lugar y asigna `Propietario` en una misma transacción.
5. `PUT /api/Negocio/{id}`: edita nombre y descripción; exige rol y propiedad,
   excepto que un administrador puede editar cualquier negocio aprobado.

Cada petición autenticada vuelve a consultar los roles en la base; una aprobación
o revocación surte efecto incluso con un token anterior. Recargar la pantalla
actualiza las opciones visibles. El backend devuelve 401 sin sesión y 403 sin
permiso. Ocultar botones en el frontend no es la barrera de autorización.

El panel visual inicial incluye solicitudes/aprobaciones y edición básica de
negocios. El backend de reportes y administración está documentado arriba; su
interfaz visual y la edición de menús/horarios quedan pendientes.

Pruebas HTTP de permisos: definir `MIUNI_AUTH_TEST_URL` con la URL de una API
actualizada y `MIUNI_ROUTING_TEST_CONNECTION` con su misma base migrada, y ejecutar
`dotnet test MiUni.Api.Tests/MiUni.Api.Tests.csproj`. Crean tres cuentas, una categoría
y negocios desechables, prueban autorización real y eliminan esos datos al terminar.
Las pruebas HTTP se omiten explícitamente si no se configuran ambas variables.
Frontend: `node --test src/services/*.test.js`, `npm run build` y `npm run lint`
desde `MiUni.Web`.

## RF-03: ruta más corta

`POST /api/Ruta` recibe coordenadas WGS84:

```json
{
  "a": { "latitud": 29.0838, "longitud": -110.9610 },
  "b": { "latitud": 29.0845, "longitud": -110.9590 },
  "radioConexionMetros": 100
}
```

Requiere PostgreSQL con las extensiones `postgis` y `pgrouting`, y caminos cargados
en `public.camino` (tabla creada por la migración inicial). El usuario de conexión
necesita SELECT sobre esa tabla y permiso TEMP sobre la base de datos.

La API conecta cada punto al extremo transitable más cercano, hasta 100 metros
por defecto (radio permitido: 1–1000 m). Construye una instantánea temporal del
grafo y ejecuta `pgr_dijkstra` sin dirección, con longitud geodésica en metros como
costo. Los caminos se consideran bidireccionales. Sólo conecta extremos con las
mismas coordenadas: los caminos deben estar divididos en cada intersección real.
No conecta automáticamente cruces geométricos ni proyecta puntos al interior de
un tramo. La construcción por solicitud es apropiada para una red de campus;
una red grande requeriría topología persistida e índices específicos.

La respuesta 200 incluye `distanciaMetros`, `geometria` GeoJSON (coordenadas
`[longitud, latitud]`), `origenConectado`, `destinoConectado` y las distancias
de conexión A/B. La distancia de ruta sólo incluye caminos, sin sumar la
separación entre los puntos solicitados y la red; esos accesos no son rutas
peatonales verificadas. Si ambos puntos conectan al mismo nodo, devuelve distancia
cero y geometría `Point`; en los demás casos devuelve `LineString` orientado de A a B.

- 400: puntos ausentes, coordenadas fuera de rango o radio inválido.
- 404: red vacía, punto fuera del radio o nodos sin conexión transitable.

Ejemplo ejecutable: `MiUni.Api/Ruta.http` (ajustar puerto y coordenadas).

Pruebas: `dotnet test MiUni.Api.Tests/MiUni.Api.Tests.csproj`. Para ejecutar también
las pruebas SQL reales, definir `MIUNI_ROUTING_TEST_CONNECTION` con una cadena
Npgsql de una base con PostGIS/pgRouting; sólo crean tablas temporales y no modifican
los caminos existentes. Sin esa variable, esas pruebas se omiten explícitamente.
