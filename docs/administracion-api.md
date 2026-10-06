# RF-05d / RF-13 — contratos del backend de administración

Base local: `http://localhost:5124`. Usar `Authorization: Bearer <token>` en las
rutas protegidas. Los DTOs se serializan en camelCase. Swagger expone los contratos;
`MiUni.Api/Administracion.http` contiene peticiones de ejemplo.

## Reportes

| Método y ruta | Permiso | Respuesta |
| --- | --- | --- |
| `POST /api/ReporteUsuario` | Autenticado | 201 + reporte |
| `GET /api/ReporteUsuario` | Autor: propios; admin: todos | 200 + página |
| `GET /api/ReporteUsuario/{id}` | Autor o admin | 200 + reporte |
| `PUT /api/ReporteUsuario/{id}` | Autor o admin; sólo pendiente | 204 |
| `PATCH /api/ReporteUsuario/{id}/estado` | Administrador | 204 |
| `DELETE /api/ReporteUsuario/{id}` | Autor: pendiente; admin: cualquier estado | 204 |
| `GET /api/ReporteUsuario/resumen` | Administrador | 200 + contadores por estado |

Creación: `{"lugarId":"UUID","motivo":"Información incorrecta"}` o
`{"resenaId":"UUID","motivo":"Comentario ofensivo"}`. Debe venir exactamente un
destino existente y asociado a un lugar activo. `motivo` es obligatorio, máximo
2000 caracteres. La sesión determina el usuario; el servidor fija estado
`Pendiente` y fecha UTC. El cliente no puede cambiar autor, destino, fecha ni
estado con POST/PUT. PUT recibe sólo `{"motivo":"Texto corregido"}`.

El mismo usuario no puede reportar dos veces el mismo lugar o reseña mientras
exista el reporte anterior, incluso si está revisado o descartado. Otro usuario
sí puede reportarlo. Índices únicos y restricción XOR protegen también escrituras
concurrentes o SQL directo. Si se elimina un reporte, puede crearse uno nuevo.

Estados JSON: **`Pendiente`, `Revisado`, `Descartado`**, siempre texto; los números
se rechazan en el cuerpo. PATCH recibe `{"estado":"Revisado"}`. El administrador
puede volver a `Pendiente` para reabrir un reporte. Cambiar el estado del reporte
no elimina ni modifica la reseña o el lugar denunciados; esta entrega no agrega
estados de moderación a `Resena`.

Filtros del listado: `estado`, `lugarId`, `resenaId`, `pagina` (desde 1) y
`tamanoPagina` (1–100, por defecto 20). El filtro de lugar incluye reportes de sus
reseñas. Orden: fecha descendente y luego ID; los registros históricos sin fecha
conservan `null` porque no se conoce la fecha original.

```json
{
  "items": [{
    "id": "UUID",
    "usuarioId": "UUID",
    "nombreUsuario": "Nombre del reportante",
    "emailUsuario": "correo@unison.mx",
    "lugarId": null,
    "resenaId": "UUID",
    "lugarReportadoId": "UUID del lugar de la reseña",
    "nombreLugar": "Biblioteca",
    "comentarioResena": "Comentario denunciado",
    "motivo": "Motivo del reporte",
    "estado": "Pendiente",
    "fechaCreacion": "2026-10-06T14:00:00Z"
  }],
  "total": 1,
  "pagina": 1,
  "tamanoPagina": 20
}
```

`lugarId` y `resenaId` mantienen la exclusividad del destino original.
`lugarReportadoId` identifica el lugar en ambos tipos de reporte.
Resumen: `[{"estado":"Pendiente","cantidad":3}, ...]`, incluyendo estados con cero.

## Lugares

| Método y ruta | Permiso | Comportamiento |
| --- | --- | --- |
| `GET /api/Lugar` | Público | Sólo activos; conserva contrato y filtros existentes |
| `GET /api/Lugar/admin?activo=false` | Administrador | Todos si se omite `activo`; puede filtrar |
| `GET /api/Lugar/admin/{id}` | Administrador | Detalle, incluso inactivo |
| `POST /api/Lugar` | Administrador | 201 + detalle administrativo |
| `PUT /api/Lugar/{id}` | Administrador | 204, reemplaza campos editables |
| `DELETE /api/Lugar/{id}` | Administrador | 204, baja lógica; no borra relaciones |

POST/PUT reciben:

```json
{
  "nombre": "Biblioteca",
  "descripcion": "Área de consulta",
  "categoriaId": "UUID",
  "latitud": 29.08311,
  "longitud": -110.96515,
  "piso": 0,
  "activo": true
}
```

Nombre obligatorio (máximo 150), descripción hasta 2000, categoría existente,
coordenadas numéricas obligatorias y válidas WGS84. `activo` vale true si se omite;
para una edición completa enviarlo explícitamente. PUT permite reactivar lugares.
Un negocio pendiente/rechazado debe aprobarse por el flujo de solicitudes antes
de activarlo. No se cambia propietario ni estado de solicitud mediante este DTO.

El detalle administrativo devuelve `id`, `nombre`, `descripcion`, `categoriaId`,
`nombreCategoria`, `latitud`, `longitud`, `piso`, `activo`, `fechaCreacion`,
`propietarioId` y `estadoSolicitud`.

## Categorías

GET `/api/Categoria` y `/api/Categoria/{id}` son públicos para el mapa y formularios.
POST `/api/Categoria`, PUT y DELETE `/api/Categoria/{id}` requieren Administrador.
POST/PUT reciben `{"nombre":"Servicios","icono":"building"}`: nombre obligatorio,
máximo 100 caracteres; icono opcional, máximo 500. POST devuelve 201 y el DTO
`{id,nombre,icono}`; PUT/DELETE devuelven 204. DELETE es físico y devuelve 409 si
existen lugares relacionados, aunque estén inactivos.

## Errores, migración y pruebas

- 400: validación, destino o categoría inválidos.
- 401: sin sesión válida; 403: rol insuficiente.
- 404: recurso inexistente o reporte ajeno a un usuario normal.
- 409: duplicado, conflicto concurrente, reporte cerrado o categoría en uso.

Aplicar desde `MiUni.Api`: `dotnet ef database update`. La migración
`ReportesYAdministracion` acepta el esquema inicial `"Estado" integer` y el esquema
original `estado estado_reporte`, preservando sus valores. Agrega fecha nullable,
índices únicos y validación de un solo destino. Si otra base ya contiene duplicados
o destinos inválidos, la migración falla sin borrar datos: deben reconciliarse antes.
La reversión convierte el estado al formato numérico de la migración inicial.

Pruebas: `dotnet test MiUni.Api.Tests/MiUni.Api.Tests.csproj`. Para las pruebas HTTP
y PostgreSQL, definir `MIUNI_AUTH_TEST_URL` y `MIUNI_ROUTING_TEST_CONNECTION` apuntando
a la API actualizada y su base migrada. Se crean y eliminan datos propios de prueba.
El panel visual queda a cargo del frontend; estos endpoints constituyen su backend.
