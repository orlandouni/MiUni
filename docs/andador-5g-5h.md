# Acceso local entre 5G, 5H y Calle del Conocimiento

Este ajuste es **opcional y manual**: hacer pull o iniciar la aplicacion no ejecuta
el SQL ni cambia la base de datos. El equipo puede iniciar el proyecto como antes.
Para reproducir el acceso se necesita la red OSM cargada y la columna `osm_way_id`.
Si no encuentra los accesos esperados, el script falla dentro de una transaccion.

Diego confirmó el 17 de noviembre de 2026 que el paso está abierto para caminar.
El mapa OSM consultado ese día no contiene ese enlace. El cálculo devuelve un
rodeo al sur porque sólo puede recorrer los tramos conectados que conoce.

`scripts/conectar-andador-5g-5h.sql` incorpora un trazado **aproximado**, por el
espacio al sur de los edificios 5G y 5H, entre el acceso OSM de 5G y Calle del
Conocimiento. Debe comprobarse sobre el terreno antes de considerarlo cartografía
definitiva. Los puntos de conexión se toman de la red existente y se parten los
tramos en sus cruces para que pgRouting pueda usarlos.

El script conserva los tramos originales como no transitables; no elimina
lugares, usuarios ni caminos. Es repetible y no contiene credenciales. Ejecutarlo
con `psql -v ON_ERROR_STOP=1 -f scripts/conectar-andador-5g-5h.sql`, usando la
conexión local correspondiente, después de respaldar `public.camino`.
Reaplicarlo después de importar OSM, ya que la importación reemplaza los tramos
OSM que se partieron para conectar este acceso. No se publica una edición en OSM.

La API y su contrato no cambian. El algoritmo sigue minimizando metros sobre los
caminos registrados; este ajuste no valida todos los posibles atajos del campus.

## Comprobacion local

Para A=(29.08164,-110.96260) y B=(29.081455,-110.96355), el recorrido
registrado paso de 273.89 m a 99.07 m. Son puntos de prueba del sector de la
captura, no una lectura exacta del GPS del usuario. Se comprobo que la polilinea
no intersecta el interior de los edificios descargados de OSM en ese sector.
Una segunda ejecucion del script no inserto ni desactivo ningun tramo adicional.
