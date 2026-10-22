# MiUni — sistema visual y rediseño

## Actualización de mascota aprobada (21 de octubre de 2026)

La mascota actual es la ilustración estática `public/mascot/miuni-buho-ilustrado.png`, presentada con `components/MascotaIlustrada.jsx`. Reemplaza al modelo animado en inicio y chat, y al render estático del panel de registro. El login conserva su ilustración fotográfica anterior. El chat mantiene el espacio pequeño aprobado (82 × 100 px en escritorio y 48 × 66 px en móvil), con su indicador de espera, pero ya no ejecuta gestos. Las páginas ya no descargan Three.js ni el GLB. Se conservan los archivos 3D anteriores como fuente de referencia, sin importarlos en la aplicación.

Esta actualización pasa la compilación, ESLint de sus componentes y `output/visual-qa/static-mascot-check.mjs`, que comprueba la imagen, el tamaño del chat y la ausencia de descargas del motor/modelo 3D. Las referencias a animaciones y sus pruebas que siguen abajo documentan la versión anterior del rediseño.

## Diagnóstico inicial

La plataforma tenía funciones conectadas, pero la presentación se fragmentaba entre páginas: azules y tamaños distintos, controles pequeños, tarjetas con la misma jerarquía y anchos poco adecuados para escritorio. El CSS global aún incluía estilos de la plantilla Vite y un tema oscuro automático que no coincidía con las páginas. En el mapa, búsqueda y ruta se superponían al campus. En el chat, el historial móvil dependía de `focus-within`, faltaba una etiqueta accesible para escribir y las sugerencias y mensajes no compartían una jerarquía clara. El búho ya constituía un elemento reconocible y se conservó.

## Dirección

Un compañero de campus: editorial, cercano y útil. Azul marino como identidad, amarillo cálido como acento y marfil para dar descanso. El mapa y la conversación tienen prioridad; la mascota acompaña sin crecer respecto al tamaño aprobado. No se añadieron cursores que interfieran con selección de texto o navegación del mapa.

## Variables CSS

Fuente única de tokens: `MiUni.Web/src/styles/design-system.css`.

| Uso | Variable | Valor |
| --- | --- | --- |
| Identidad y navegación | `--navy` | `#102e50` |
| Acciones y enlaces | `--blue` | `#245bdf` |
| Acento | `--yellow` | `#f4ca64` |
| Estados y orientación | `--green` | `#287967` |
| Fondo | `--canvas` | `#f7f6f2` |
| Texto | `--ink` | `#182f48` |
| Texto secundario | `--muted` | `#5e6f80` |
| Bordes | `--line` | `#dce2e8` |
| Errores | `--danger` | `#ae3b37` |

- Dos familias: Barlow Condensed local (600/700) para títulos y Segoe UI/sistema para texto. Se carga solo el subconjunto latino.
- Espaciado: `--space-1` a `--space-16`, desde 4 a 64 px en múltiplos de 4.
- Radios: 12, 18, 24 px y píldora. Sombras discretas para campos, burbujas y superficies flotantes.
- Botones compartidos: `.ui-button`, `.secondary` y `.yellow`, con altura base de 46 px.
- Movimiento: 160/240/320 ms y `cubic-bezier(.22,1,.36,1)`. Las animaciones de interfaz usan transform y opacidad; se respetan las preferencias de movimiento reducido. El mapa y el modelo conservan sus sistemas específicos de renderizado.

## Archivos y responsabilidades

| Archivos | Responsabilidad |
| --- | --- |
| `src/styles/design-system.css`, `base.css`, `src/index.css` | Tokens, base limpia, foco visible, enlaces de salto y movimiento reducido |
| `src/styles/shell.css`, `pages/Home.jsx`, `Home.css`, `MapaCampus.jsx` | Navegación, hero, accesos en franja, portada y mapa de escritorio |
| `components/CampusMap.jsx`, `CampusMap.css`, `mapMarkers.js`, `src/styles/map.css` | Panel lateral de búsqueda/categorías, resultados, ficha de selección y marcadores |
| `components/LugarDesdeChat.jsx`, `RutaMapa.jsx` | Presentación coherente de lugares enlazados y respeto de movimiento reducido al encuadrar rutas |
| `pages/Chat.jsx`, `Chat.css`, `src/styles/chat.css` | Historial móvil explícito, sugerencias, burbujas, espera, error, compositor y mascota pequeña |
| `components/PageMotion.jsx`, `routes/AppRoutes.jsx` | Transición de entrada y revelado al hacer scroll; mismas rutas, cargadas por demanda |
| `components/AuthExplorer.jsx`, `AuthExplorer.css`, `pages/CampusPanel.jsx`, `Auth.css` | Acceso y registro con marca, mapa ilustrativo y mascota estática |
| `pages/Comunidad.css`, `Negocios.css`, `Admin.css`, `NotFound.jsx`, `NotFound.css` | Coherencia visual de páginas secundarias y estado 404 |
| `public/mascot/miuni-fallback.png` | Render transparente del modelo real para la espera del 3D; no es una nueva versión del personaje |

Las APIs, validaciones, permisos, rutas y backend se conservan. No se añadieron servicios ni dependencias de producto.

## Mascota

Se mantienen los gestos aprobados: Saludo en inicio, Reposo durante la consulta, Asentir tras una respuesta y Negar ante errores o una declaración explícita de falta de información. Al terminar una reacción, vuelve a reposo. El chatbot sigue con un espacio de 82 × 100 px en escritorio y 48 × 66 px en móvil. El modelo se carga al estar visible, comparte la descarga y pausa el renderizado fuera de pantalla. El respaldo transparente procede del mismo GLB.

## Rendimiento y librerías

Se reutilizan React/Router, Leaflet/React Leaflet para navegación geográfica, Lucide para iconos y Three.js para el modelo animado. No se incorporó una librería de animación. La entrada principal de JS pasó de aproximadamente 554 KB a 285 KB sin comprimir; mapa, páginas y motor 3D quedan separados. El bloque diferido de Three.js aún genera el aviso de tamaño de Vite (>500 KB).

Playwright Core solo se instaló en `output/visual-qa/` para verificar en Chrome aislado; no forma parte de las dependencias ni del código de la aplicación. El navegador integrado de herramientas no pudo inicializarse durante esta revisión.

## Verificación

- Compilación de producción, con copia de recursos públicos, realizada correctamente.
- 18 pruebas existentes de autenticación, roles, lugares, rutas y gestos: correctas.
- ESLint de los archivos JS/JSX modificados: correcto. El repositorio ya tenía una advertencia/error de `react-hooks/set-state-in-effect` en `pages/Admin.jsx` que queda fuera del cambio visual.
- Pruebas de navegador a 1366 × 768 y 1920 × 1080; adaptación a 390 px, más revisión específica a 768 y 320 px.
- Selección de categorías/lugares, elección de origen/destino, cálculo de ruta, sugerencias, espera, respuesta, falta de información y error del chat comprobados con respuestas API simuladas. No se alteraron datos reales del servidor.
- Capturas y reporte en `output/visual-qa/screenshots/` y `output/visual-qa/report.json`. Las capturas con lugares y mensajes usan datos de prueba; no representan una validación de disponibilidad del backend real.

Para repetir: iniciar Vite en el puerto 5174 y ejecutar `node output/visual-qa/check.mjs` y `node output/visual-qa/motion-check.mjs` en Windows con Chrome instalado.
