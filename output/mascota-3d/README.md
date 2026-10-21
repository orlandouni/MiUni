# Búho MiUni · Primera versión 3D

Modelo original construido con geometría, sin archivos de Meshy ni imágenes generadas como sustituto del volumen.

- `miuni-buho.glb`: modelo real con materiales y felpa geométrica simplificada; se abre en Blender o visores compatibles con GLB.
- `index.html`: vista interactiva; requiere un servidor local, no abrir directamente con doble clic.
- `owl.js`: fuente editable del personaje.
- `preview.js`: controles de cámara, expresiones de prueba y saludo.
- `export.mjs`: regenera el GLB ejecutando `node export.mjs` desde esta carpeta.

Las expresiones y el saludo son pruebas del visor y no están grabadas como animaciones en el GLB. El archivo exportado contiene la pose base y partes separadas. El pelo es una aproximación geométrica corta y la forma no reproduce exactamente la imagen de referencia. Vista separada para aprobación; no integrada al inicio ni al chatbot.
