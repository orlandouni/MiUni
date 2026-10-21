# Sprint 8 — integración chat → mapa

El módulo está conectado en `Chat.jsx`, debajo de los mensajes con rol `assistant`, tanto nuevos como recuperados del historial. No modifica el contrato de la API ni requiere RAG o librerías adicionales.

La API consulta el catálogo de lugares activos en cada pregunta y envía al servicio
del chatbot únicamente sus nombres y categorías, sin descripciones. Lo hace en
las respuestas normales y en streaming. El modelo recibe instrucciones para
relacionar actividades con lugares y conservar los nombres completos al responder;
el frontend convierte esas menciones en enlaces por ID al mapa. Los nuevos lugares
activos quedan incluidos automáticamente. El historial conserva la pregunta original,
sin el catálogo. Las respuestas antiguas no se regeneran: para obtener las nuevas
recomendaciones hay que volver a preguntar. La selección depende del modelo; el catálogo
no acredita horarios, acceso ni deportes que no se puedan determinar por el nombre.

## Integración para Arturo

## Ejecutar después de hacer pull

Se necesitan Node.js compatible con Vite 8, .NET 10 y PostgreSQL con la configuración y extensiones del README principal. No se añadieron dependencias npm/NuGet ni rutas absolutas de una computadora.

1. En `MiUni.Web`, ejecutar `npm ci` y `npm run dev`. El proxy existente apunta a la API en `http://localhost:5124`.
2. Configurar la conexión PostgreSQL y JWT de cada entorno en `MiUni.Api/appsettings.Development.json` (ignorado por Git). Desde `MiUni.Api`, ejecutar `dotnet ef database update` si hay migraciones pendientes; requiere la herramienta `dotnet-ef` compatible con .NET 10. En particular deben estar aplicadas `ReportesYAdministracion` y `RepararRolesHistorialChat`. Estos cambios de interfaz y borrado no agregan migraciones.
3. Iniciar la API con `dotnet run --project MiUni.Api --launch-profile http` desde la raíz. Reiniciarla después de actualizar para habilitar `DELETE /api/Chat/sessions/{id}`. El endpoint elimina solo los mensajes del usuario autenticado en esa conversación.
4. Clonar por separado `https://github.com/angluisb/MiUni-RAG.git`. Seguir su README, instalar `requirements.txt` en un entorno virtual y configurar `API_KEY` en su `.env` privado. Iniciar `uvicorn app:app --host 127.0.0.1 --port 8000`. La API .NET usa `Rag:BaseUrl`; ajustar esa configuración si FastAPI está en otro host/puerto.
5. Para disponer de los 51 lugares, ejecutar `seed_lugares.sql` de la raíz contra la misma base PostgreSQL configurada en la API, después de las migraciones. Puede hacerse desde la herramienta de consultas de pgAdmin o con `psql -h localhost -U postgres -d MiUni -v ON_ERROR_STOP=1 --single-transaction -f seed_lugares.sql`, ajustando conexión y nombre de base. El script omite lugares existentes por nombre. Hacer pull no importa los datos automáticamente.

Esta actualización no añade dependencias ni migraciones ni requiere cambios en
MiUni-RAG. Reiniciar la API .NET tras compilar y recargar el frontend para activarla.

Cada integrante aporta su configuración privada; ninguna clave se incluye en este cambio. El archivo `MiUni.Web/src/services/ApiKey.env` no es leído por la aplicación y no es necesario para ejecutarla. No configurar la clave de Gemini como variable pública `VITE_*`.

El historial y los mensajes tienen scroll independiente. El encabezado de bienvenida se oculta cuando hay mensajes. La papelera pide confirmación antes de eliminar una conversación; no se borran conversaciones al actualizar el código.

### Componente de enlaces

Nota para compartir: «Ya conectamos `LugaresDelMensaje` en `Chat.jsx`. El componente busca nombres completos en el catálogo y genera los enlaces; el mapa recibe `/mapa?lugar=ID`. Falta probar juntos una conversación real con los servicios activos. No necesitas añadir otra instancia del componente ni instalar dependencias. Al volver del mapa, la conversación guardada se puede recuperar desde el historial; la selección automática de la conversación no está implementada».

Importar `LugaresDelMensaje` desde `src/components/LugaresDelMensaje.jsx` y colocarlo debajo de cada mensaje **del asistente**, dentro del router existente:

```jsx
<LugaresDelMensaje texto={mensaje.contenido} />
```

Para la respuesta inmediata de `POST /api/Chat`, el texto está en `respuesta.answer`; en el historial está en `mensaje.contenido`. Para streaming, preferir montar el componente cuando termine la respuesta.

El componente consulta `GET /api/Lugar`. Si el chat ya carga el catálogo, compartirlo evita consultas por mensaje:

```jsx
<LugaresDelMensaje texto={mensaje.contenido} lugares={catalogo} />
```

Los nombres completos se comparan ignorando acentos, mayúsculas y separadores. No hay interpretación semántica, aliases ni coordenadas inventadas. Si dos lugares tienen el mismo nombre, ambos pueden aparecer; el catálogo debe permitir distinguirlos. No se infiere una ubicación a partir de una mención genérica como «biblioteca».

## Enlace al mapa

`/mapa?lugar=ID` consulta el catálogo público de lugares activos y centra el lugar con zoom 18, mostrando el marcador y su popup. No se usa un endpoint de detalle inexistente. Se mantiene visible aunque los filtros anteriores no coincidan. La vista de búsqueda no sobrescribe el centrado mientras el enlace está activo. Cambiar la búsqueda/categoría o pulsar «Volver a la búsqueda» abandona la selección del chat. El marcador permite elegir origen/destino o abrir las reseñas.

Se informa si el lugar no existe, fue desactivado, carece de coordenadas válidas o falla la API. La selección no altera la URL de otros parámetros. El chat sigue siendo responsable de preservar su historial al navegar.

## Validación independiente

Desde `MiUni.Web`:

```sh
node --test src/services/lugaresChat.test.mjs src/components/LugaresDelMensaje.test.mjs
npm run build
```

Las siete pruebas usan mensajes y catálogo simulados, sin servidor, Gemini ni autenticación. Cubren detección, límites de palabras, duplicados, coordenadas, enlaces, consultas concurrentes y recuperación tras errores. La prueba de componente compila el JSX real con Vite y verifica los enlaces renderizados dentro del router; no simula clics ni comprueba visualmente Leaflet.

Cuando estén disponibles el backend y el chat:

1. Responder con un nombre exacto del catálogo y pulsar su enlace: debe centrar y abrir el marcador.
2. Repetir con varios lugares, acentos y mayúsculas; una respuesta sin coincidencias no debe mostrar enlaces.
3. Abrir directamente `/mapa?lugar=ID` y recargar: debe funcionar sin pasar por el chat.
4. Probar un filtro incompatible, luego abrir el enlace: el lugar debe seguir visible. Cambiar el filtro debe devolver el control a la búsqueda.
5. Probar un ID inexistente y una API inaccesible; comprobar los mensajes de error.
6. Elegir origen/destino desde el marcador y verificar que las rutas siguen funcionando.

Estas pruebas reales de navegador siguen pendientes; la verificación automatizada de lógica y compilación no sustituye la prueba integrada con el chat.
