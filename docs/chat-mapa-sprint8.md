# Sprint 8 — integración chat → mapa

El módulo funciona independientemente del chat. No modifica el contrato de la API ni requiere RAG o librerías adicionales. La interfaz real de Arturo todavía debe conectarlo.

## Integración para Arturo

Nota para compartir: «Ya está preparada la integración del sprint 8 de chat a mapa. Después de actualizar tu rama, coloca `LugaresDelMensaje` debajo de las respuestas del asistente y pásale el texto. El componente busca nombres completos en el catálogo y genera los enlaces; el mapa ya recibe `/?lugar=ID`. Conserva el historial del chat al navegar. Nos falta probar juntos una conversación real; no necesitas cambiar el backend ni instalar dependencias por este módulo».

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

`/?lugar=ID` consulta el catálogo público de lugares activos y centra el lugar con zoom 18, mostrando el marcador y su popup. No se usa un endpoint de detalle inexistente. Se mantiene visible aunque los filtros anteriores no coincidan. La vista de búsqueda no sobrescribe el centrado mientras el enlace está activo. Cambiar la búsqueda/categoría o pulsar «Volver a la búsqueda» abandona la selección del chat. El marcador permite elegir origen/destino o abrir las reseñas.

Se informa si el lugar no existe, fue desactivado, carece de coordenadas válidas o falla la API. La selección no altera la URL de otros parámetros. El chat sigue siendo responsable de preservar su historial al navegar.

## Validación independiente

Desde `MiUni.Web`:

```sh
node --test src/services/lugaresChat.test.mjs
npm run build
```

Las pruebas usan mensajes y catálogo simulados, sin servidor, Gemini ni autenticación. Cubren detección, límites de palabras, duplicados, coordenadas, enlaces, consultas concurrentes y recuperación tras errores.

Cuando estén disponibles el backend y el chat:

1. Responder con un nombre exacto del catálogo y pulsar su enlace: debe centrar y abrir el marcador.
2. Repetir con varios lugares, acentos y mayúsculas; una respuesta sin coincidencias no debe mostrar enlaces.
3. Abrir directamente `/?lugar=ID` y recargar: debe funcionar sin pasar por el chat.
4. Probar un filtro incompatible, luego abrir el enlace: el lugar debe seguir visible. Cambiar el filtro debe devolver el control a la búsqueda.
5. Probar un ID inexistente y una API inaccesible; comprobar los mensajes de error.
6. Elegir origen/destino desde el marcador y verificar que las rutas siguen funcionando.

Estas pruebas reales de navegador siguen pendientes; la verificación automatizada de lógica y compilación no sustituye la prueba integrada con el chat.
