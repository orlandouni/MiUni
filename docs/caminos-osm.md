# Caminos desde OpenStreetMap

La red de rutas (`public.camino`) se genera a partir de OpenStreetMap (OSM) en lugar
de dibujarse a mano. El importador vive en la propia API y no agrega dependencias.

## Importar o actualizar

```powershell
powershell -NoProfile -File .\scripts\dev.ps1 -Action migrate   # agrega camino.osm_way_id
powershell -NoProfile -File .\scripts\dev.ps1 -Action caminos
```

Equivale a `dotnet run --no-launch-profile -- --importar-caminos-osm campus` desde
`MiUni.Api` con la conexión configurada. Opciones:

| Opción | Uso |
| --- | --- |
| `--importar-caminos-osm campus` | Área por defecto: campus de la Unison y ~500 m alrededor (29.0755,-110.975 a 29.0915,-110.9535). |
| `--importar-caminos-osm 'sur,oeste,norte,este'` | Otra área, en grados decimales (máximo unos 10 km × 10 km). |
| `--osm-incluir-privados true` | Incluye vías con `access=private` (por ejemplo, si el campus está etiquetado así). |
| `--osm-overpass-url <url>` | Otro servidor Overpass si el principal está saturado. |

Volver a correrlo es seguro: en una sola transacción borra sólo los caminos con
`osm_way_id` y carga los nuevos. Los caminos manuales (`osm_way_id` nulo) se
conservan; el importador avisa cuántos quedan, porque sólo se conectan a la red si
sus extremos coinciden exactamente con un nodo OSM. Si la descarga falla o no
trae caminos, la red existente no se modifica.

## Qué se importa

- Vías `highway` caminables: `footway`, `path`, `pedestrian`, `steps`, `corridor`,
  `cycleway`, `track`, `living_street`, `residential`, `service`, `unclassified`,
  `tertiary`, `secondary`, `primary` (y sus `_link`) y `road`.
- Se excluyen autopistas y vías rápidas (`motorway`, `trunk`), `foot=no`,
  `foot=use_sidepath`, `access=no` y `access=private` (salvo `foot=yes`,
  `designated`, `permissive` o `destination`, o con `--osm-incluir-privados`).
- Cada vía se corta en los nodos que comparte con otra vía, como necesita pgRouting.
- Se conserva sólo la red conectada principal. Los tramos sueltos (un andador dentro
  de un edificio sin conexión) se descartan para que no "atrapen" el origen o
  destino y la API conteste que no hay ruta. El total descartado se muestra al final.
- `surface` se guarda en `tipo_superficie` y el id de la vía en `osm_way_id`.

## Si falta un camino

1. Comprobar en <https://www.openstreetmap.org> si el andador existe en la zona.
2. Si no existe o está mal conectado, agregarlo o corregirlo en OSM con el editor
   web (requiere cuenta gratuita). Beneficia a cualquier mapa que use OSM.
3. Esperar unos minutos a que Overpass lo refleje y volver a correr `-Action caminos`.

Como respaldo temporal se puede insertar un camino manual en `public.camino`, cuidando
que sus extremos tengan exactamente las coordenadas de un nodo OSM.

## Licencia

Los datos de OSM son © colaboradores de OpenStreetMap, bajo licencia ODbL. El mapa
ya muestra esa atribución en las teselas; las rutas derivadas deben conservarla.
