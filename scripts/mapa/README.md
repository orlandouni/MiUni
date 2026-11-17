# Lugares del mapa (seed_lugares.sql)

Los lugares se cargan con la migración `MiUni.Api/Migrations/20261117160000_LugaresCampus.cs`.
Esa migración y `seed_lugares.sql` (el mismo SQL, para revisarlo o correrlo con psql)
se generan con:

```bash
python scripts/mapa/generar_seed_lugares.py
```

Fuentes:

- **Google Maps (seed original)**: los 51 lugares que ya tenía el seed.
- **OpenStreetMap**: `lugares_osm.csv` es un extracto de la API Overpass del
  2026-11-17 (solo filas útiles para el campus; se quitaron calles, cajones de
  estacionamiento y negocios fuera de la Unison). Cada fila trae su tipo e id;
  el lugar se puede revisar en `https://www.openstreetmap.org/<tipo>/<id>`.
  Datos © colaboradores de OpenStreetMap, licencia ODbL.

Consulta usada (POST a `https://overpass-api.de/api/interpreter`, campo `data`):

```
[out:csv(::type,::id,::lat,::lon,name,ref,amenity,building,shop,leisure,sport,"addr:housenumber";true;"|")][timeout:90][bbox:29.079,-110.9712,29.088,-110.9572];(nwr[name];nwr[amenity];nwr[shop];nwr[leisure];nwr[building][ref];nwr[building]["addr:housenumber"];);out center;
```

Reglas del generador:

- Un edificio de OSM cuya clave (5K, 9Q1, 7M…) ya está en el seed original no se
  repite. Los que son el mismo lugar con otro nombre están en `YA_EN_SEED`.
- Cuartos de máquinas, bodegas y plantas de servicio no se agregan (`NO_SON_DESTINO`).
- Casetas de comida y estacionamientos sin nombre se nombran por el edificio más
  cercano ("Caseta de alimentos junto al 3K3").
- Categorías nuevas: Laboratorio, Baños, Estacionamiento y Plazas y áreas verdes.

Para agregar más lugares después de que la migración ya esté en `main`, no la regeneres:
las bases que ya la aplicaron no la vuelven a correr. Crea una migración nueva con los
lugares nuevos (el SQL es idempotente: omite los nombres que ya existen).
