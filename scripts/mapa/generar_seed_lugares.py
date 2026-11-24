"""Genera seed_lugares.sql: los 51 lugares originales + lugares de OpenStreetMap.

Uso:  python scripts/mapa/generar_seed_lugares.py
Escribe scripts/mapa/seed_lugares.sql y la migración MiUni.Api/Migrations/20261117160000_LugaresCampus.cs,
que es como se cargan los lugares: `dotnet ef database update`.

Entrada: scripts/mapa/lugares_osm.csv, exportado de la API Overpass el 2026-11-17
(bbox del campus 29.079,-110.9712,29.088,-110.9572; ver scripts/mapa/README.md).
Cada lugar nuevo lleva su fuente (osm.org/<tipo>/<id>) en la columna `fuente`.
Los datos de OpenStreetMap son © colaboradores de OpenStreetMap, licencia ODbL.
"""
import csv
import math
import os
import re

AQUI = os.path.dirname(os.path.abspath(__file__))

# ---------------------------------------------------------------- originales
# (nombre, categoria, lat, lon) tal como estaban en el seed anterior (Google Maps).
ORIGINALES = [
    ('Biblioteca Central', 'Biblioteca', 29.083336885926236, -110.9631235545639),
    ('Edificio 5K - Ingeniería Industrial', 'Edificio académico', 29.082433958767652, -110.96229774960754),
    ('Comedor Universitario', 'Comida', 29.083479582078365, -110.96239170625606),
    ('8A - Dirección de servicios escolares', 'Oficinas', 29.08301087233202, -110.96397283719133),
    ('Estadio Miguel Castro Servin', 'Deportes', 29.08152792769758, -110.96447112827282),
    ('Biblioteca de ciencias químicas', 'Biblioteca', 29.08078644738234, -110.96196620554912),
    ('Edificio 8B', 'Edificio académico', 29.08319702361846, -110.96476271806604),
    ('Edificio 9Q2 Contabilidad y administración', 'Edificio académico', 29.083943871292664, -110.96515117407203),
    ('Edificio 9Q1 Contabilidad', 'Edificio académico', 29.08436322188771, -110.96506434272939),
    ('Caffenio Kfresa', 'Comida', 29.083814712391263, -110.96412488120325),
    ('Caffenio Kfriki', 'Comida', 29.082610900887037, -110.96177177427644),
    ('Departamento de publicaciones', 'Oficinas', 29.082283092004737, -110.96548923003093),
    ('EDIFICIO 3J CENTRO DE LAS ARTES', 'Edificio académico', 29.08104729834615, -110.95879242887841),
    ('Galería del Centro de las Artes Unison', 'Edificio académico', 29.081023552937978, -110.95920225418605),
    ('Milla Unison', 'Deportes', 29.08398064036948, -110.96737389702028),
    ('Edificio 7M Cultura física y deporte', 'Edificio académico', 29.085555799669134, -110.96764333476813),
    ('Campos de futbol rápido', 'Deportes', 29.08442801839946, -110.96781442079354),
    ('Canchas de basquetbol', 'Deportes', 29.08387177826846, -110.9684153785543),
    ('Canchas de tenis', 'Deportes', 29.08342419414103, -110.96931896356874),
    ('Campo de futbol de la milla', 'Deportes', 29.08298393243921, -110.96832139940093),
    ('Campo de beisbol Jesús Ramon Álvarez Valenzuela', 'Deportes', 29.083797708713583, -110.96667281897432),
    ('Estadio Gustavo Hodgers', 'Deportes', 29.08301714793128, -110.96731894845881),
    ('Campo de softball Vázquez', 'Deportes', 29.08296732469288, -110.96908155174916),
    ('Campo de softball 1 Unison', 'Deportes', 29.082126444207418, -110.96879410258457),
    ('Alberca universitaria', 'Deportes', 29.081636442001916, -110.96686768073089),
    ('Edificio 7T Oficina manzana 07', 'Oficinas', 29.082175470616065, -110.967495375696),
    ('Clinica odontología', 'Edificio académico', 29.081830173196654, -110.96803912463099),
    ('Edificio 7K Odontología', 'Edificio académico', 29.081764678005356, -110.96777366212731),
    ('Anfiteatros y laboratorios', 'Edificio académico', 29.081435874669328, -110.96802267918576),
    ('7L', 'Edificio académico', 29.081307333389407, -110.96815859510545),
    ('Departamento de medicina y ciencias de la salud', 'Oficinas', 29.080976255527865, -110.96806490657706),
    ('7E', 'Edificio académico', 29.08132147132004, -110.9684470358794),
    ('Biblioteca de medicina', 'Biblioteca', 29.08135266473295, -110.96864659352471),
    ('Posgrado en biociencias', 'Edificio académico', 29.0814504985619, -110.96904895370176),
    ('Edificio 7H Ciencias biológicas y de la salud', 'Edificio académico', 29.081507213781443, -110.96910087113908),
    ('Dictus Unison', 'Oficinas', 29.081249839672594, -110.96942188576311),
    ('Edificio 7G Dictus', 'Edificio académico', 29.08112738561198, -110.96941493773623),
    ('Edificio 7J', 'Edificio académico', 29.081027195817782, -110.969079116437),
    ('Dirección de investigación y posgrado', 'Oficinas', 29.080731890416782, -110.9666677364078),
    ('Biblioteca de posgrado', 'Biblioteca', 29.08051547487882, -110.96695974463448),
    ('Edificio 7A Departamento de Maestria en administración', 'Edificio académico', 29.08049247877641, -110.96632748999278),
    ('Edificio 7B Gimnasio Universitario', 'Deportes', 29.085574086901385, -110.96709840732544),
    ('9B Departamento de Administración Publica y Sociología', 'Edificio académico', 29.0861605775537, -110.96553166884021),
    ('Biblioteca de Ciencias sociales', 'Biblioteca', 29.086416668022864, -110.96512121609511),
    ('Biblioteca de sociología y administración publica', 'Biblioteca', 29.086178921081814, -110.96533619825723),
    ('9N', 'Edificio académico', 29.086147679137127, -110.9657808443354),
    ('9M', 'Edificio académico', 29.085973895684297, -110.96579425074938),
    ('Edificio 9A Historia y antropología', 'Edificio académico', 29.08646986116994, -110.96490495859699),
    ('Edificio 9C Psicologia y ciencias de la comunicación', 'Edificio académico', 29.08589559955791, -110.96539747076906),
    ('9D', 'Edificio académico', 29.085643111129798, -110.96529308136306),
    ('Departamento ciencias de la comunicación', 'Oficinas', 29.08567835471582, -110.965336512985),
]

# ---------------------------------------------------------------- reglas OSM
# Elementos de OSM que ya están en el seed original con otro nombre (mismo lugar).
YA_EN_SEED = {
    'way/424837465',   # 5A2 = Biblioteca de ciencias químicas
    'way/426916920',   # 12F = Biblioteca Central
    'way/426916398',   # 12D = Comedor Universitario
    'way/648546362',   # Caffenio = Caffenio Kfriki
    'way/649611652',   # Caffenio = Caffenio Kfresa
    'way/253353265',   # Alberca Unison = Alberca universitaria
    'way/253353262',   # Campo de Sóftbol = Campo de softball 1 Unison
    'way/644696440',   # Campo de Beisbol = Campo de softball Vázquez
    'way/424636551',   # Campo de Béisbol = Estadio Gustavo Hodgers
    'way/645129120',   # campo de béisbol = Campo de beisbol Jesús Ramón Álvarez
    'way/1300063189',  # Estadio Miguel Castro Servín
    'way/424833762',   # cancha dentro del Estadio Miguel Castro Servín
    'way/424667479',   # 7C = Departamento de medicina y ciencias de la salud
    'way/424667713',   # 7D = Anfiteatros y laboratorios
    'way/661932645',   # futbol = Campos de futbol rápido
    'way/1300082337',  # futbol = Campos de futbol rápido
    'way/1095283811',  # pista = Milla Unison
    'way/661932642',   # Unidad Deportiva (área que ya cubren sus campos)
    'way/658985699',   # "UNISON Lenguas Extranjeras" = edificios 13A y 13B
    'way/3420657734', 'way/334943501', 'node/3420657734',  # Banorte (fuera del campus)
}
# Canchas sueltas de la unidad deportiva: ya hay un pin por grupo en el seed.
CANCHAS_AGRUPADAS = {f'way/{i}' for i in range(253353267, 253353281)}
# Cuartos de máquinas, bodegas y servicios internos: no sirven como destino.
NO_SON_DESTINO = {'3X', '3Y', '7R', '7O', '7L', '6B', '7Q', '6G'}

BIBLIOTECAS = {'3K1', '3T', '10J', '1A'}
OFICINAS = {'2A', '3B', '3U', '3V', '3W', '6C', '6I', '8C', '9J', '9L', '9Q3', '11A', '11B', '14A'}

PLAZAS = 'Plazas y áreas verdes'
# Claves que no son edificios de aulas: se nombran por lo que son.
NOMBRES_ESPECIALES = {
    '7N': 'Baños de la alberca (7N)',
    '6H': 'Campo de tiro con arco y atletismo (6H)',
}


def distancia(a, b):
    return math.hypot((a[0] - b[0]) * 111320, (a[1] - b[1]) * 111320 * math.cos(math.radians(29.08)))


def leer_osm():
    with open(os.path.join(AQUI, 'lugares_osm.csv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='|'):
            r['name'] = r['name'].strip()
            r['ref_osm'] = f"{r['@type']}/{r['@id']}"
            r['pos'] = (float(r['@lat']), float(r['@lon']))
            yield r


def codigo_y_resto(nombre):
    m = re.match(r'^\(?(\d{1,2}[A-Z]{1,2}) ?(\d?)\)?\s*(.*)$', nombre)
    if not m or nombre.startswith('(0'):
        return None, nombre
    return m[1] + m[2], m[3].strip()


def codigos_originales():
    cods = set()
    for n, *_ in ORIGINALES:
        cods.update(re.findall(r'\b(\d{1,2}[A-Z]\d?)\b', n))
    return cods


def categoria_edificio(cod, resto):
    if cod in BIBLIOTECAS:
        return 'Biblioteca'
    if cod in OFICINAS:
        return 'Oficinas'
    if cod == '7I':
        return 'Comida'
    if cod == '7N':
        return 'Baños'
    if cod == '6H':
        return 'Deportes'
    if re.search(r'Laborator|Taller|Observación', resto) and 'Aulas' not in resto:
        return 'Laboratorio'
    return 'Edificio académico'


def main():
    filas = [(n, c, la, lo, 'Google Maps (seed original)') for n, c, la, lo in ORIGINALES]
    usados = {n.lower() for n, *_ in ORIGINALES}
    cods_orig = codigos_originales()
    osm = list(leer_osm())

    # Edificios con código, para nombrar casetas y estacionamientos "junto al X".
    edificios = []
    for r in osm:
        cod, _ = codigo_y_resto(r['name'])
        if cod and r['amenity'] != 'parking' and cod not in NO_SON_DESTINO:
            edificios.append((cod, r['pos']))

    def cercano(pos):
        return min(edificios, key=lambda e: distancia(e[1], pos))[0]

    def agregar(nombre, cat, r):
        base, n = nombre, 2
        while nombre.lower() in usados:
            nombre = f'{base} ({n})'
            n += 1
        usados.add(nombre.lower())
        filas.append((nombre, cat, r['pos'][0], r['pos'][1], f"OpenStreetMap {r['ref_osm']}"))

    for r in osm:
        ref, nombre = r['ref_osm'], r['name']
        if ref in YA_EN_SEED or ref in CANCHAS_AGRUPADAS:
            continue
        cod, resto = codigo_y_resto(nombre)
        if r['amenity'] in ('parking',):
            etiqueta = re.match(r'^\((\d\d-EM\d+)\)$', nombre)
            if nombre.startswith('(8E)'):
                agregar('Estacionamiento multinivel 8E', 'Estacionamiento', r)
            elif etiqueta:
                agregar(f'Estacionamiento {etiqueta[1]}', 'Estacionamiento', r)
            else:
                agregar(f'Estacionamiento junto al {cercano(r["pos"])}', 'Estacionamiento', r)
        elif r['amenity'] == 'fast_food':
            agregar(f'Caseta de alimentos junto al {cercano(r["pos"])}', 'Comida', r)
        elif cod:
            if cod in cods_orig or cod in NO_SON_DESTINO:
                continue
            resto = resto.strip('"').rstrip('.')
            nombre_lugar = NOMBRES_ESPECIALES.get(cod, f'Edificio {cod} - {resto}')
            agregar(nombre_lugar, categoria_edificio(cod, resto), r)
        elif r['leisure'] in ('park', 'garden'):
            if nombre:
                agregar(nombre.replace('Del ', 'del '), PLAZAS, r)
        elif r['leisure'] == 'pitch':
            deporte = {'basketball': 'Cancha de basquetbol', 'beachvolleyball': 'Cancha de voleibol de playa',
                       'baseball': 'Campo de béisbol', 'soccer': 'Cancha de futbol'}.get(r['sport'])
            if nombre:
                agregar(nombre if 'Béisbol' not in nombre else 'Campo de béisbol (poniente)', 'Deportes', r)
            elif deporte:
                agregar(f'{deporte} junto al {cercano(r["pos"])}', 'Deportes', r)
        elif r['leisure'] == 'fitness_station':
            agregar('Gimnasio al aire libre', 'Deportes', r)
        elif r['leisure'] == 'sports_hall' and nombre:
            agregar('Gimnasio Status Unison', 'Deportes', r)
        elif r['amenity'] == 'studio':
            agregar('Radio Universidad 107.5 FM', 'Oficinas', r)
        elif nombre == 'Instituto de Geología (UNAM)':
            agregar('Instituto de Geología (UNAM), Estación Regional del Noroeste', 'Edificio académico', r)

    ancho = min(max(len(sql_texto(f[0])) for f in filas) + 1, 72)
    lineas = []
    for n, c, la, lo, fu in filas:
        lineas.append(f"  ({sql_texto(n) + ',':<{ancho}} {sql_texto(c) + ',':<26} {la:.7f}, {lo:.7f}, {sql_texto(fu)})")
    cuerpo = SQL_CUERPO + '\n' + ',\n'.join(lineas) + '\n' + SQL_FIN
    raiz = os.path.dirname(os.path.dirname(AQUI))
    with open(os.path.join(AQUI, 'seed_lugares.sql'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(SQL_CABECERA.format(total=len(filas)) + '\n' + cuerpo + '\n' + SQL_VERIFICAR.format(total=len(filas)) + '\n')
    ruta = os.path.join(raiz, 'MiUni.Api', 'Migrations', '20261117160000_LugaresCampus.cs')
    with open(ruta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(MIGRACION.replace('@@TOTAL@@', str(len(filas))).replace('@@SQL@@', cuerpo))
    print(f'{len(filas)} lugares -> scripts/mapa/seed_lugares.sql y {os.path.relpath(ruta, raiz)}')


def sql_texto(s):
    return "'" + s.replace("'", "''") + "'"


SQL_CABECERA = """-- =========================================================
-- MiUni: categorías y lugares del campus Hermosillo, Universidad de Sonora
-- ({total} lugares). ARCHIVO GENERADO: no editar a mano, sino
--   python scripts/mapa/generar_seed_lugares.py
--
-- REQUISITO: tener las migraciones aplicadas
--   (cd MiUni.Api && dotnet ef database update)
--
-- Se puede ejecutar varias veces sin duplicar nada: los lugares
-- que ya existen (por nombre) se omiten y solo se agregan los nuevos.
-- Coordenadas como (latitud, longitud); abajo se invierten para PostGIS.
--
-- Fuente de cada lugar en la última columna:
--   * "Google Maps (seed original)": los 51 lugares del seed anterior.
--   * "OpenStreetMap way/123": https://www.openstreetmap.org/way/123
--     (nombre oficial del edificio con su clave, p. ej. 5A1). Datos
--     © colaboradores de OpenStreetMap, licencia ODbL.
-- ========================================================="""

SQL_CUERPO = """-- 1) Categorías
UPDATE categoria
SET nombre = 'Edificio académico', icono = 'edificio-academico.svg'
WHERE nombre = 'Ingeniería';

INSERT INTO categoria (nombre, icono)
SELECT v.nombre, v.icono
FROM (VALUES
  ('Edificio académico',    'edificio-academico.svg'),
  ('Laboratorio',           'laboratorio.svg'),
  ('Biblioteca',            'biblioteca.svg'),
  ('Comida',                'comida.svg'),
  ('Oficinas',              'oficinas.svg'),
  ('Deportes',              'deportes.svg'),
  ('Baños',                 'banos.svg'),
  ('Estacionamiento',       'estacionamiento.svg'),
  ('Plazas y áreas verdes', 'plaza.svg')
) AS v(nombre, icono)
WHERE NOT EXISTS (SELECT 1 FROM categoria c WHERE c.nombre = v.nombre);

-- 2) Lugares
INSERT INTO lugar (nombre, categoria_id, ubicacion, activo)
SELECT
  v.nombre,
  c.id,
  ST_SetSRID(ST_MakePoint(v.lon, v.lat), 4326)::geography,
  true
FROM (VALUES"""

SQL_FIN = """) AS v(nombre, categoria, lat, lon, fuente)
JOIN categoria c ON c.nombre = v.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = v.nombre);"""

SQL_VERIFICAR = """
-- 3) Verificar: en una BD limpia deben aparecer estos {total} lugares
SELECT c.nombre AS categoria, count(*) AS lugares
FROM lugar l JOIN categoria c ON c.id = l.categoria_id
GROUP BY c.nombre
ORDER BY c.nombre;"""

MIGRACION = """// ARCHIVO GENERADO por scripts/mapa/generar_seed_lugares.py: no editar a mano.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <summary>
    /// Carga los @@TOTAL@@ lugares del campus (los 51 del seed original y los de OpenStreetMap)
    /// y sus categorías. Idempotente: omite los lugares que ya existen por nombre, así que
    /// sirve igual en una base vacía que en una donde ya se corrió seed_lugares.sql.
    /// Fuente de cada lugar: columna `fuente` del SQL (ver scripts/mapa/README.md).
    /// </summary>
    [DbContext(typeof(MiUniDbContext))]
    [Migration("20261117160000_LugaresCampus")]
    public partial class LugaresCampus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(\"\"\"\"
@@SQL@@
\"\"\"\");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin cambios: los lugares pueden tener reseñas, favoritos o fotos de usuarios,
            // así que revertir la migración no los borra.
        }
    }
}
"""

if __name__ == '__main__':
    main()
