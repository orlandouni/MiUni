-- =========================================================
-- MiUni: categorías y lugares del campus (51 lugares)
--
-- REQUISITO: tener las migraciones aplicadas
--   (cd MiUni.Api && dotnet ef database update)
--
-- Se puede ejecutar varias veces sin duplicar nada: los lugares
-- que ya existen (por nombre) se omiten y solo se agregan los nuevos.
-- Las coordenadas están como (latitud, longitud), tal como salen
-- en Google Maps; abajo se invierten a (lon, lat) para PostGIS.
-- =========================================================

-- 1) Categorías
-- Si la BD tiene la categoría antigua "Ingeniería", se renombra
-- (los lugares que ya la usan se actualizan solos).
UPDATE categoria
SET nombre = 'Edificio académico', icono = 'edificio-academico.svg'
WHERE nombre = 'Ingeniería';

INSERT INTO categoria (nombre, icono)
SELECT v.nombre, v.icono
FROM (VALUES
  ('Edificio académico', 'edificio-academico.svg'),
  ('Biblioteca',         'biblioteca.svg'),
  ('Comida',             'comida.svg'),
  ('Oficinas',           'oficinas.svg'),
  ('Deportes',           'deportes.svg')
) AS v(nombre, icono)
WHERE NOT EXISTS (SELECT 1 FROM categoria c WHERE c.nombre = v.nombre);

-- 2) Lugares
INSERT INTO lugar (nombre, categoria_id, ubicacion, activo)
SELECT
  v.nombre,
  c.id,
  ST_SetSRID(ST_MakePoint(v.lon, v.lat), 4326)::geography,
  true
FROM (VALUES
  -- Lugares originales
  ('Biblioteca Central',                              'Biblioteca',         29.083336885926236, -110.9631235545639),
  ('Edificio 5K - Ingeniería Industrial',             'Edificio académico', 29.082433958767652, -110.96229774960754),
  ('Comedor Universitario',                           'Comida',             29.083479582078365, -110.96239170625606),

  -- Lote 1
  ('8A - Dirección de servicios escolares',           'Oficinas',           29.08301087233202,  -110.96397283719133),
  ('Estadio Miguel Castro Servin',                    'Deportes',           29.08152792769758,  -110.96447112827282),
  ('Biblioteca de ciencias químicas',                 'Biblioteca',         29.08078644738234,  -110.96196620554912),
  ('Edificio 8B',                                     'Edificio académico', 29.08319702361846,  -110.96476271806604),
  ('Edificio 9Q2 Contabilidad y administración',      'Edificio académico', 29.083943871292664, -110.96515117407203),
  ('Edificio 9Q1 Contabilidad',                       'Edificio académico', 29.08436322188771,  -110.96506434272939),
  ('Caffenio Kfresa',                                 'Comida',             29.083814712391263, -110.96412488120325),
  ('Caffenio Kfriki',                                 'Comida',             29.082610900887037, -110.96177177427644),
  ('Departamento de publicaciones',                   'Oficinas',           29.082283092004737, -110.96548923003093),
  ('EDIFICIO 3J CENTRO DE LAS ARTES',                 'Edificio académico', 29.08104729834615,  -110.95879242887841),
  ('Galería del Centro de las Artes Unison',          'Edificio académico', 29.081023552937978, -110.95920225418605),

  -- Lote 2: zona deportiva y ciencias de la salud (manzana 7)
  ('Milla Unison',                                    'Deportes',           29.08398064036948,  -110.96737389702028),
  ('Edificio 7M Cultura física y deporte',            'Edificio académico', 29.085555799669134, -110.96764333476813),
  ('Campos de futbol rápido',                         'Deportes',           29.08442801839946,  -110.96781442079354),
  ('Canchas de basquetbol',                           'Deportes',           29.08387177826846,  -110.9684153785543),
  ('Canchas de tenis',                                'Deportes',           29.08342419414103,  -110.96931896356874),
  ('Campo de futbol de la milla',                     'Deportes',           29.08298393243921,  -110.96832139940093),
  ('Campo de beisbol Jesús Ramon Álvarez Valenzuela', 'Deportes',           29.083797708713583, -110.96667281897432),
  ('Estadio Gustavo Hodgers',                         'Deportes',           29.08301714793128,  -110.96731894845881),
  ('Campo de softball Vázquez',                       'Deportes',           29.08296732469288,  -110.96908155174916),
  ('Campo de softball 1 Unison',                      'Deportes',           29.082126444207418, -110.96879410258457),
  ('Alberca universitaria',                           'Deportes',           29.081636442001916, -110.96686768073089),
  ('Edificio 7T Oficina manzana 07',                  'Oficinas',           29.082175470616065, -110.967495375696),
  ('Clinica odontología',                             'Edificio académico', 29.081830173196654, -110.96803912463099),
  ('Edificio 7K Odontología',                         'Edificio académico', 29.081764678005356, -110.96777366212731),
  ('Anfiteatros y laboratorios',                      'Edificio académico', 29.081435874669328, -110.96802267918576),
  ('7L',                                              'Edificio académico', 29.081307333389407, -110.96815859510545),
  ('Departamento de medicina y ciencias de la salud', 'Oficinas',           29.080976255527865, -110.96806490657706),
  ('7E',                                              'Edificio académico', 29.08132147132004,  -110.9684470358794),
  ('Biblioteca de medicina',                          'Biblioteca',         29.08135266473295,  -110.96864659352471),
  ('Posgrado en biociencias',                         'Edificio académico', 29.0814504985619,   -110.96904895370176),
  ('Edificio 7H Ciencias biológicas y de la salud',   'Edificio académico', 29.081507213781443, -110.96910087113908),
  ('Dictus Unison',                                   'Oficinas',           29.081249839672594, -110.96942188576311),
  ('Edificio 7G Dictus',                              'Edificio académico', 29.08112738561198,  -110.96941493773623),
  ('Edificio 7J',                                     'Edificio académico', 29.081027195817782, -110.969079116437),
  ('Dirección de investigación y posgrado',           'Oficinas',           29.080731890416782, -110.9666677364078),
  ('Biblioteca de posgrado',                          'Biblioteca',         29.08051547487882,  -110.96695974463448),
  ('Edificio 7A Departamento de Maestria en administración', 'Edificio académico', 29.08049247877641, -110.96632748999278),
  ('Edificio 7B Gimnasio Universitario',              'Deportes',           29.085574086901385, -110.96709840732544),

  -- Lote 2: zona de ciencias sociales y humanidades (manzana 9)
  ('9B Departamento de Administración Publica y Sociología', 'Edificio académico', 29.0861605775537, -110.96553166884021),
  ('Biblioteca de Ciencias sociales',                 'Biblioteca',         29.086416668022864, -110.96512121609511),
  ('Biblioteca de sociología y administración publica', 'Biblioteca',       29.086178921081814, -110.96533619825723),
  ('9N',                                              'Edificio académico', 29.086147679137127, -110.9657808443354),
  ('9M',                                              'Edificio académico', 29.085973895684297, -110.96579425074938),
  ('Edificio 9A Historia y antropología',             'Edificio académico', 29.08646986116994,  -110.96490495859699),
  ('Edificio 9C Psicologia y ciencias de la comunicación', 'Edificio académico', 29.08589559955791, -110.96539747076906),
  ('9D',                                              'Edificio académico', 29.085643111129798, -110.96529308136306),
  ('Departamento ciencias de la comunicación',        'Oficinas',           29.08567835471582,  -110.965336512985)
) AS v(nombre, categoria, lat, lon)
JOIN categoria c ON c.nombre = v.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = v.nombre);

-- 3) Verificar: en una BD limpia deben aparecer estos 51 lugares
SELECT l.nombre, c.nombre AS categoria,
       ST_Y(l.ubicacion::geometry) AS lat,
       ST_X(l.ubicacion::geometry) AS lon
FROM lugar l JOIN categoria c ON c.id = l.categoria_id
ORDER BY c.nombre, l.nombre;
