# Origen por ubicación del dispositivo

> Desde noviembre de 2026 el mapa funciona como una app de mapas: el origen es tu
> ubicación, un toque en el mapa (o elegir un lugar) marca el destino y la ruta se
> calcula sola. Con el GPS activo se recalcula cuando te mueves 15 m o más, sin
> volver a encuadrar el mapa. «Cambiar» o «Salir desde aquí» fijan un origen manual.
> Lo de abajo describe el permiso y el seguimiento, que no cambiaron.

Al entrar al mapa se solicita el permiso nativo del navegador. Si ya está concedido,
el seguimiento se inicia automáticamente. Se recuerda sólo que ya se intentó pedir
el permiso (localStorage), nunca una autorización ni coordenadas. Si se deniega o
se descarta la solicitud, no se insiste al volver; el botón permite reintentarlo.
La duración del permiso depende del navegador: un permiso temporal puede requerir
activarlo otra vez. Sin Permissions API se evita repetir la solicitud automática
y se ofrece el botón. No hay confirmación adicional de MiUni.

El usuario puede cancelar, elegir A/B en el mapa o usar «Desde aquí» / «Ir aquí».
Detener, limpiar, elegir un origen manual o salir del mapa ejecuta clearWatch y descarta resultados tardíos. Elegir destino o calcular una ruta mantiene el seguimiento.
Cancelar no puede cerrar el permiso nativo del navegador; descarta su resultado.
La precisión aparece en metros y como círculo. Por encima de 100 m se recomienda
seleccionar el origen manualmente. No se limita artificialmente el GPS al campus:
la API conserva su validación de conexión a caminos y su mensaje de ruta no disponible.

Se mantiene POST /api/Ruta con a, b y radioConexionMetros=100. Sólo se envían
latitud y longitud al calcular; precisión y consentimiento no se envían.
La función no persiste coordenadas ni registra posiciones. El seguimiento actualiza el marcador A y la precisión; no recalcula rutas automáticamente. La ruta conserva el origen usado al calcular y muestra un aviso si la posición cambia. Al detener se conserva la última posición, indicada como detenida. Un error detiene el seguimiento y permite reactivarlo desde el botón. El navegador puede limitar las actualizaciones en segundo plano.
En producción se necesita HTTPS y permitir geolocalización en la política del
sitio/navegador. Localhost funciona como contexto seguro. No hay nuevas dependencias,
migraciones ni variables de entorno. El usuario puede necesitar reactivar un permiso
denegado en la configuración de su navegador.

## Comprobación manual

- Abrir /mapa por primera vez: debe aparecer sólo el permiso nativo.
- Volver con permiso concedido: seguimiento automático. Denegar o descartar y volver: no insistir.
- Autorizar: comprobar marcador A, precisión y conservar el destino B elegido.
- Simular varias posiciones: A y su precisión deben actualizarse. Seleccionar B y calcular debe mantener el seguimiento. Tras otro movimiento, conservar el recorrido anterior y mostrar el aviso para recalcular.
- Denegar, simular timeout o ubicación no disponible: poder elegir A manualmente.
- Detener, limpiar, elegir otro origen o salir mientras se localiza: una respuesta
  tardía no debe cambiar la selección.
- Probar precisión mayor de 100 m y ubicación fuera de la red: advertencia y
  posibilidad de cambiar el origen, sin presentar un camino inexistente.
- Repetir selección manual, «Desde aquí» e «Ir aquí», y uso por teclado/móvil.
