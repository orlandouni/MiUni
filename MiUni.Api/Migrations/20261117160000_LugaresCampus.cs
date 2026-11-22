// ARCHIVO GENERADO por scripts/mapa/generar_seed_lugares.py: no editar a mano.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiUni.Api.Models;

#nullable disable

namespace MiUni.Api.Migrations
{
    /// <summary>
    /// Carga los 182 lugares del campus (los 51 del seed original y los de OpenStreetMap)
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
            migrationBuilder.Sql(""""
-- 1) Categorías
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
FROM (VALUES
  ('Biblioteca Central',                                                    'Biblioteca',              29.0833369, -110.9631236, 'Google Maps (seed original)'),
  ('Edificio 5K - Ingeniería Industrial',                                   'Edificio académico',      29.0824340, -110.9622977, 'Google Maps (seed original)'),
  ('Comedor Universitario',                                                 'Comida',                  29.0834796, -110.9623917, 'Google Maps (seed original)'),
  ('8A - Dirección de servicios escolares',                                 'Oficinas',                29.0830109, -110.9639728, 'Google Maps (seed original)'),
  ('Estadio Miguel Castro Servin',                                          'Deportes',                29.0815279, -110.9644711, 'Google Maps (seed original)'),
  ('Biblioteca de ciencias químicas',                                       'Biblioteca',              29.0807864, -110.9619662, 'Google Maps (seed original)'),
  ('Edificio 8B',                                                           'Edificio académico',      29.0831970, -110.9647627, 'Google Maps (seed original)'),
  ('Edificio 9Q2 Contabilidad y administración',                            'Edificio académico',      29.0839439, -110.9651512, 'Google Maps (seed original)'),
  ('Edificio 9Q1 Contabilidad',                                             'Edificio académico',      29.0843632, -110.9650643, 'Google Maps (seed original)'),
  ('Caffenio Kfresa',                                                       'Comida',                  29.0838147, -110.9641249, 'Google Maps (seed original)'),
  ('Caffenio Kfriki',                                                       'Comida',                  29.0826109, -110.9617718, 'Google Maps (seed original)'),
  ('Departamento de publicaciones',                                         'Oficinas',                29.0822831, -110.9654892, 'Google Maps (seed original)'),
  ('EDIFICIO 3J CENTRO DE LAS ARTES',                                       'Edificio académico',      29.0810473, -110.9587924, 'Google Maps (seed original)'),
  ('Galería del Centro de las Artes Unison',                                'Edificio académico',      29.0810236, -110.9592023, 'Google Maps (seed original)'),
  ('Milla Unison',                                                          'Deportes',                29.0839806, -110.9673739, 'Google Maps (seed original)'),
  ('Edificio 7M Cultura física y deporte',                                  'Edificio académico',      29.0855558, -110.9676433, 'Google Maps (seed original)'),
  ('Campos de futbol rápido',                                               'Deportes',                29.0844280, -110.9678144, 'Google Maps (seed original)'),
  ('Canchas de basquetbol',                                                 'Deportes',                29.0838718, -110.9684154, 'Google Maps (seed original)'),
  ('Canchas de tenis',                                                      'Deportes',                29.0834242, -110.9693190, 'Google Maps (seed original)'),
  ('Campo de futbol de la milla',                                           'Deportes',                29.0829839, -110.9683214, 'Google Maps (seed original)'),
  ('Campo de beisbol Jesús Ramon Álvarez Valenzuela',                       'Deportes',                29.0837977, -110.9666728, 'Google Maps (seed original)'),
  ('Estadio Gustavo Hodgers',                                               'Deportes',                29.0830171, -110.9673189, 'Google Maps (seed original)'),
  ('Campo de softball Vázquez',                                             'Deportes',                29.0829673, -110.9690816, 'Google Maps (seed original)'),
  ('Campo de softball 1 Unison',                                            'Deportes',                29.0821264, -110.9687941, 'Google Maps (seed original)'),
  ('Alberca universitaria',                                                 'Deportes',                29.0816364, -110.9668677, 'Google Maps (seed original)'),
  ('Edificio 7T Oficina manzana 07',                                        'Oficinas',                29.0821755, -110.9674954, 'Google Maps (seed original)'),
  ('Clinica odontología',                                                   'Edificio académico',      29.0818302, -110.9680391, 'Google Maps (seed original)'),
  ('Edificio 7K Odontología',                                               'Edificio académico',      29.0817647, -110.9677737, 'Google Maps (seed original)'),
  ('Anfiteatros y laboratorios',                                            'Edificio académico',      29.0814359, -110.9680227, 'Google Maps (seed original)'),
  ('7L',                                                                    'Edificio académico',      29.0813073, -110.9681586, 'Google Maps (seed original)'),
  ('Departamento de medicina y ciencias de la salud',                       'Oficinas',                29.0809763, -110.9680649, 'Google Maps (seed original)'),
  ('7E',                                                                    'Edificio académico',      29.0813215, -110.9684470, 'Google Maps (seed original)'),
  ('Biblioteca de medicina',                                                'Biblioteca',              29.0813527, -110.9686466, 'Google Maps (seed original)'),
  ('Posgrado en biociencias',                                               'Edificio académico',      29.0814505, -110.9690490, 'Google Maps (seed original)'),
  ('Edificio 7H Ciencias biológicas y de la salud',                         'Edificio académico',      29.0815072, -110.9691009, 'Google Maps (seed original)'),
  ('Dictus Unison',                                                         'Oficinas',                29.0812498, -110.9694219, 'Google Maps (seed original)'),
  ('Edificio 7G Dictus',                                                    'Edificio académico',      29.0811274, -110.9694149, 'Google Maps (seed original)'),
  ('Edificio 7J',                                                           'Edificio académico',      29.0810272, -110.9690791, 'Google Maps (seed original)'),
  ('Dirección de investigación y posgrado',                                 'Oficinas',                29.0807319, -110.9666677, 'Google Maps (seed original)'),
  ('Biblioteca de posgrado',                                                'Biblioteca',              29.0805155, -110.9669597, 'Google Maps (seed original)'),
  ('Edificio 7A Departamento de Maestria en administración',                'Edificio académico',      29.0804925, -110.9663275, 'Google Maps (seed original)'),
  ('Edificio 7B Gimnasio Universitario',                                    'Deportes',                29.0855741, -110.9670984, 'Google Maps (seed original)'),
  ('9B Departamento de Administración Publica y Sociología',                'Edificio académico',      29.0861606, -110.9655317, 'Google Maps (seed original)'),
  ('Biblioteca de Ciencias sociales',                                       'Biblioteca',              29.0864167, -110.9651212, 'Google Maps (seed original)'),
  ('Biblioteca de sociología y administración publica',                     'Biblioteca',              29.0861789, -110.9653362, 'Google Maps (seed original)'),
  ('9N',                                                                    'Edificio académico',      29.0861477, -110.9657808, 'Google Maps (seed original)'),
  ('9M',                                                                    'Edificio académico',      29.0859739, -110.9657943, 'Google Maps (seed original)'),
  ('Edificio 9A Historia y antropología',                                   'Edificio académico',      29.0864699, -110.9649050, 'Google Maps (seed original)'),
  ('Edificio 9C Psicologia y ciencias de la comunicación',                  'Edificio académico',      29.0858956, -110.9653975, 'Google Maps (seed original)'),
  ('9D',                                                                    'Edificio académico',      29.0856431, -110.9652931, 'Google Maps (seed original)'),
  ('Departamento ciencias de la comunicación',                              'Oficinas',                29.0856784, -110.9653365, 'Google Maps (seed original)'),
  ('Campo de béisbol (poniente)',                                           'Deportes',                29.0822644, -110.9700500, 'OpenStreetMap way/424636472'),
  ('Estacionamiento junto al 7K',                                           'Estacionamiento',         29.0821694, -110.9679732, 'OpenStreetMap way/424637459'),
  ('Estacionamiento junto al 7B',                                           'Estacionamiento',         29.0848232, -110.9671285, 'OpenStreetMap way/424638722'),
  ('Instituto de Geología (UNAM), Estación Regional del Noroeste',          'Edificio académico',      29.0813879, -110.9705174, 'OpenStreetMap way/424648335'),
  ('Estacionamiento junto al 7K (2)',                                       'Estacionamiento',         29.0817913, -110.9675310, 'OpenStreetMap way/424648962'),
  ('Estacionamiento junto al 7C',                                           'Estacionamiento',         29.0809416, -110.9677856, 'OpenStreetMap way/424648964'),
  ('Edificio 7I - Cafetería y Librería',                                    'Comida',                  29.0815082, -110.9682359, 'OpenStreetMap way/424667714'),
  ('Edificio 7F - Unidad Integral de Posgrados',                            'Edificio académico',      29.0805871, -110.9669429, 'OpenStreetMap way/424667971'),
  ('Edificio 7P - Edificio Campo de Béisbol Hodgers',                       'Edificio académico',      29.0825586, -110.9677595, 'OpenStreetMap way/424669315'),
  ('Estacionamiento junto al 7G',                                           'Estacionamiento',         29.0815294, -110.9706092, 'OpenStreetMap way/424669560'),
  ('Estacionamiento 06-EM102',                                              'Estacionamiento',         29.0807102, -110.9639069, 'OpenStreetMap way/424834395'),
  ('Edificio 6C - Talleres Gráficos',                                       'Oficinas',                29.0820448, -110.9655468, 'OpenStreetMap way/424834770'),
  ('Edificio 5P - DIPA',                                                    'Edificio académico',      29.0805872, -110.9631386, 'OpenStreetMap way/424837176'),
  ('Edificio 5A1 - Ingeniería Química y Metalurgia. Ciencias Químico Biológicas y DIPA', 'Edificio académico',      29.0806764, -110.9625655, 'OpenStreetMap way/424837177'),
  ('Edificio 5S - Laboratorio de Células Madres',                           'Laboratorio',             29.0805513, -110.9623335, 'OpenStreetMap way/424838007'),
  ('Edificio 5Q - Ingeniería Química y Metalurgia. Laboratorios',           'Laboratorio',             29.0812528, -110.9632756, 'OpenStreetMap way/424839838'),
  ('Edificio 5D - Ingeniería Química y Metalurgia. Aulas',                  'Edificio académico',      29.0811151, -110.9627507, 'OpenStreetMap way/424839894'),
  ('Edificio 5B - Auditorio Rivera Zamudio. Ingeniería Química y Metalurgia. Jefatura', 'Edificio académico',      29.0812250, -110.9622359, 'OpenStreetMap way/424840150'),
  ('Edificio 5E - CS. Químico Biológicas. Oficinas y Aulas. ING. Química Metalurgia. Aulas', 'Edificio académico',      29.0813818, -110.9628070, 'OpenStreetMap way/424840600'),
  ('Edificio 5F - Aulas y Laboratorios de IQM y CQB',                       'Edificio académico',      29.0814604, -110.9623065, 'OpenStreetMap way/424840675'),
  ('Edificio 5H - DIPA. Laboratorios',                                      'Laboratorio',             29.0815943, -110.9631762, 'OpenStreetMap way/424840826'),
  ('Edificio 5G - Ingeniería Industrial',                                   'Edificio académico',      29.0816444, -110.9626850, 'OpenStreetMap way/424840888'),
  ('Edificio 5N - Ciencias Químico Biológicas. Laboratorios',               'Laboratorio',             29.0818982, -110.9622638, 'OpenStreetMap way/424840950'),
  ('Edificio 5I - Ingeniería Química y Metalurgia',                         'Edificio académico',      29.0817638, -110.9633003, 'OpenStreetMap way/424844018'),
  ('Edificio 5J - Ingeniería Industrial',                                   'Edificio académico',      29.0818593, -110.9628797, 'OpenStreetMap way/424844815'),
  ('Edificio 5O - Ingeniería Industrial',                                   'Edificio académico',      29.0821271, -110.9628625, 'OpenStreetMap way/424844816'),
  ('Estacionamiento junto al 5O',                                           'Estacionamiento',         29.0820719, -110.9631276, 'OpenStreetMap way/424844817'),
  ('Edificio 5M - División Ingeniería, Ing. Industrial. Aulas y Cubículos', 'Edificio académico',      29.0821912, -110.9623593, 'OpenStreetMap way/424844840'),
  ('Edificio 5L - Depto. Ingeniería Civil y Minas (Lic. En Minas)',         'Edificio académico',      29.0823759, -110.9628165, 'OpenStreetMap way/424845265'),
  ('Edificio 5R - Ing. Industrial, Mecatrónica. Aulas y Laboratorios',      'Edificio académico',      29.0823366, -110.9633116, 'OpenStreetMap way/424845266'),
  ('Baños de la alberca (7N)',                                              'Baños',                   29.0821130, -110.9671405, 'OpenStreetMap way/425015262'),
  ('Edificio 3K1 - Div. de Cs. Exactas y Naturales - Biblioteca Departamento de Matemáticas', 'Biblioteca',              29.0807963, -110.9608967, 'OpenStreetMap way/425024372'),
  ('Edificio 3K4 - Ciencias de la Computación',                             'Edificio académico',      29.0807171, -110.9605153, 'OpenStreetMap way/425411814'),
  ('Edificio 3K2 - Matemáticas. Cubículos y Centro de Cómputo',             'Edificio académico',      29.0810643, -110.9610909, 'OpenStreetMap way/425415404'),
  ('Estacionamiento 03-EM104',                                              'Estacionamiento',         29.0809325, -110.9599719, 'OpenStreetMap way/426235939'),
  ('Estacionamiento 03-EM101',                                              'Estacionamiento',         29.0816111, -110.9588644, 'OpenStreetMap way/426238655'),
  ('Caseta de alimentos junto al 3J',                                       'Comida',                  29.0811192, -110.9591606, 'OpenStreetMap way/426238981'),
  ('Estacionamiento 03-EM105',                                              'Estacionamiento',         29.0815985, -110.9605667, 'OpenStreetMap way/426246124'),
  ('Edificio 3N - Música',                                                  'Edificio académico',      29.0819920, -110.9595482, 'OpenStreetMap way/426247017'),
  ('Edificio 3T - División de Humanidades y Bellas Artes, Biblioteca y Artes Plásticas', 'Biblioteca',              29.0821658, -110.9590322, 'OpenStreetMap way/426247427'),
  ('Edificio 3W - DICTUS. Oficinas',                                        'Oficinas',                29.0823034, -110.9592942, 'OpenStreetMap way/426692742'),
  ('Edificio 3U - Direcciones de: Infraestructura, Adquisiciones y Patrimonio, Investigación y Posgrado, Desarrollo Académico', 'Oficinas',                29.0825210, -110.9589118, 'OpenStreetMap way/426693583'),
  ('Edificio 3B - Consultorio Médico - Recursos Humanos - CULTUREST',       'Oficinas',                29.0827416, -110.9590491, 'OpenStreetMap way/426693584'),
  ('Edificio 3L - DIFUS. Cubículos',                                        'Edificio académico',      29.0815098, -110.9604575, 'OpenStreetMap way/426693847'),
  ('Edificio 3I - DIFUS. Administración, Aulas y Laboratorios',             'Edificio académico',      29.0814648, -110.9608244, 'OpenStreetMap way/426694018'),
  ('Edificio 3M - DIFUS. Laboratorio de Estado Sólido',                     'Laboratorio',             29.0814294, -110.9613044, 'OpenStreetMap way/426694019'),
  ('Edificio 3S - DIFUS. Laboratorio de Ionización',                        'Laboratorio',             29.0814079, -110.9615651, 'OpenStreetMap way/426694020'),
  ('Edificio 3H - DIFUS. Estación de Observación Solar',                    'Laboratorio',             29.0816844, -110.9605988, 'OpenStreetMap way/426694092'),
  ('Edificio 3G - Polímeros. Oficinas, Laboratorios y Cubículo',            'Laboratorio',             29.0816417, -110.9611661, 'OpenStreetMap way/426694252'),
  ('Edificio 3F - Física. Oficinas y Cubículos',                            'Edificio académico',      29.0817779, -110.9613187, 'OpenStreetMap way/426694253'),
  ('Edificio 3P - Geología. Laboratorios y Cubículos',                      'Laboratorio',             29.0818921, -110.9605923, 'OpenStreetMap way/426694254'),
  ('Edificio 3R - Física. Laboratorios y Cubículos',                        'Laboratorio',             29.0821558, -110.9604269, 'OpenStreetMap way/426694299'),
  ('Edificio 3Q - Letras y Lingüística. Aulas',                             'Edificio académico',      29.0823557, -110.9604075, 'OpenStreetMap way/426694435'),
  ('Edificio 3D - Física. Taller de Máquinas y Herramientas',               'Laboratorio',             29.0821013, -110.9608971, 'OpenStreetMap way/426694436'),
  ('Edificio 3E - Física. Aulas, Laboratorios, Centro de Cómputo',          'Edificio académico',      29.0820452, -110.9612836, 'OpenStreetMap way/426694463'),
  ('Edificio 3C - Geología',                                                'Edificio académico',      29.0822463, -110.9613165, 'OpenStreetMap way/426694464'),
  ('Edificio 3V - Prestaciones. Oficinas',                                  'Oficinas',                29.0826644, -110.9610834, 'OpenStreetMap way/426694953'),
  ('Estacionamiento junto al 3V',                                           'Estacionamiento',         29.0824776, -110.9611043, 'OpenStreetMap way/426695111'),
  ('Estacionamiento multinivel 8E',                                         'Estacionamiento',         29.0828224, -110.9654842, 'OpenStreetMap way/426913157'),
  ('Edificio 8C - Direcciones: Informática - Innovación Educativa',         'Oficinas',                29.0831694, -110.9648988, 'OpenStreetMap way/426913940'),
  ('Edificio 8D - Mecatrónica. Aulas y Laboratorios',                       'Edificio académico',      29.0832914, -110.9643937, 'OpenStreetMap way/426915134'),
  ('Edificio 12E - Ingeniería Civil. Laboratorio',                          'Laboratorio',             29.0828350, -110.9635613, 'OpenStreetMap way/426915853'),
  ('Edificio 12B - Ingeniería Civil. Laboratorio',                          'Laboratorio',             29.0827827, -110.9632241, 'OpenStreetMap way/426915854'),
  ('Edificio 12A - Ingeniería Civil. Jefatura y Aulas',                     'Edificio académico',      29.0828322, -110.9626025, 'OpenStreetMap way/426916223'),
  ('Edificio 12C - Ingeniería Civil. Aulas y Cubículos',                    'Edificio académico',      29.0830939, -110.9626129, 'OpenStreetMap way/426916224'),
  ('Edificio 2A - Edificio Principal. Rectoría y Adm. Central',             'Oficinas',                29.0834619, -110.9604937, 'OpenStreetMap way/426918203'),
  ('Edificio 11B - Vicerrectoría Unidad Regional Centro',                   'Oficinas',                29.0841177, -110.9608412, 'OpenStreetMap way/426919266'),
  ('Edificio 11A - Vigilancia - Almacén General',                           'Oficinas',                29.0840865, -110.9620103, 'OpenStreetMap way/426919669'),
  ('Edificio 1A - Museo Unison y Biblioteca',                               'Biblioteca',              29.0837307, -110.9580923, 'OpenStreetMap way/426921663'),
  ('Edificio 14A - Promoción Financiera',                                   'Oficinas',                29.0855753, -110.9592243, 'OpenStreetMap way/426922510'),
  ('Estacionamiento junto al 13B',                                          'Estacionamiento',         29.0801386, -110.9590377, 'OpenStreetMap way/426922838'),
  ('Edificio 16A - Centro de Desarrollo Infantil',                          'Edificio académico',      29.0802839, -110.9597400, 'OpenStreetMap way/426922839'),
  ('Edificio 13A - Lenguas Extranjeras. Aulas y CAALE',                     'Edificio académico',      29.0790844, -110.9590568, 'OpenStreetMap way/426922971'),
  ('Edificio 13B - Lenguas Extranjeras. Jefatura y Aulas',                  'Edificio académico',      29.0795256, -110.9591513, 'OpenStreetMap way/426922972'),
  ('Edificio 10H - Economía. Aulas',                                        'Edificio académico',      29.0839516, -110.9626698, 'OpenStreetMap way/426923766'),
  ('Edificio 10L - Negocios y Comercios Internacionales',                   'Edificio académico',      29.0839156, -110.9630153, 'OpenStreetMap way/426923767'),
  ('Edificio 10G - Contabilidad y Administración. Aulas',                   'Edificio académico',      29.0838696, -110.9634157, 'OpenStreetMap way/426923768'),
  ('Edificio 10J - División de Ciencias Económicas y Administrativas - Economía. Biblioteca', 'Biblioteca',              29.0842605, -110.9625935, 'OpenStreetMap way/426923844'),
  ('Edificio 10E - Contabilidad y Administración. Aulas',                   'Edificio académico',      29.0842424, -110.9630600, 'OpenStreetMap way/426923845'),
  ('Edificio 10F - Contabilidad y Administración. Cubículos',               'Edificio académico',      29.0841252, -110.9634308, 'OpenStreetMap way/426923846'),
  ('Edificio 10D - Enfermería y Trabajo Social. Aulas',                     'Edificio académico',      29.0853922, -110.9630742, 'OpenStreetMap way/426926470'),
  ('Edificio 10C - Trabajo Social. Jefatura y Aulas',                       'Edificio académico',      29.0853905, -110.9637867, 'OpenStreetMap way/426926471'),
  ('Edificio 10B - Enfermería. Jefatura y Aulas',                           'Edificio académico',      29.0856351, -110.9632247, 'OpenStreetMap way/426926472'),
  ('Edificio 10K - División de Ciencias Biológicas y De la Salud - Enfermería', 'Edificio académico',      29.0857886, -110.9632537, 'OpenStreetMap way/426926542'),
  ('Edificio 10A - Enfermería. Aulas',                                      'Edificio académico',      29.0857922, -110.9637877, 'OpenStreetMap way/426926543'),
  ('Edificio 9Q3 - Secretaría Administrativa. Salas de Contabilidad y Administración. Jefatura', 'Oficinas',                29.0840246, -110.9646061, 'OpenStreetMap way/426926653'),
  ('Estacionamiento junto al 9Q2',                                          'Estacionamiento',         29.0838759, -110.9657856, 'OpenStreetMap way/427101863'),
  ('Estacionamiento junto al 9H',                                           'Estacionamiento',         29.0850794, -110.9652392, 'OpenStreetMap way/427104279'),
  ('Cancha de basquetbol junto al 9D',                                      'Deportes',                29.0854001, -110.9658612, 'OpenStreetMap way/427104280'),
  ('Estacionamiento junto al 9N',                                           'Estacionamiento',         29.0861638, -110.9659410, 'OpenStreetMap way/427104567'),
  ('Edificio 9P - Psicología y Ciencias de la Comunicación',                'Edificio académico',      29.0846384, -110.9655020, 'OpenStreetMap way/427106408'),
  ('Edificio 9J - Bufete Jurídico',                                         'Oficinas',                29.0846265, -110.9650956, 'OpenStreetMap way/427106409'),
  ('Edificio 9L - Bufete Jurídico',                                         'Oficinas',                29.0846246, -110.9647714, 'OpenStreetMap way/427106411'),
  ('Edificio 9K - Posgrado en Derecho',                                     'Edificio académico',      29.0848180, -110.9645682, 'OpenStreetMap way/427106412'),
  ('Edificio 9I - Ciencias de la Comunicación. Cubículos y Talleres',       'Laboratorio',             29.0849975, -110.9654332, 'OpenStreetMap way/427107147'),
  ('Edificio 9H - Maestría en Psicología',                                  'Edificio académico',      29.0851688, -110.9652994, 'OpenStreetMap way/427107148'),
  ('Edificio 9E - Psicología y Ciencias de la Comunicación',                'Edificio académico',      29.0854151, -110.9652231, 'OpenStreetMap way/427108885'),
  ('Edificio 9G - Psicología',                                              'Edificio académico',      29.0854098, -110.9645534, 'OpenStreetMap way/427108886'),
  ('Edificio 9O - Sociología y Administración Pública. Aulas',              'Edificio académico',      29.0862731, -110.9654077, 'OpenStreetMap way/427108890'),
  ('Plaza de los 100 años',                                                 'Plazas y áreas verdes',   29.0827655, -110.9582180, 'OpenStreetMap way/558911994'),
  ('Plaza del Estudiante',                                                  'Plazas y áreas verdes',   29.0833140, -110.9615376, 'OpenStreetMap way/643182628'),
  ('Plaza Emiliana de Zubeldia',                                            'Plazas y áreas verdes',   29.0835488, -110.9592257, 'OpenStreetMap way/644604137'),
  ('Plaza de Letras',                                                       'Plazas y áreas verdes',   29.0826884, -110.9596461, 'OpenStreetMap way/644604155'),
  ('Cancha de voleibol de playa junto al 7M',                               'Deportes',                29.0847231, -110.9683292, 'OpenStreetMap way/644696441'),
  ('Gimnasio al aire libre',                                                'Deportes',                29.0852264, -110.9680604, 'OpenStreetMap way/644709848'),
  ('Edificio 9F - Psicologia y Ciencias de la Comunicación',                'Edificio académico',      29.0851466, -110.9648085, 'OpenStreetMap way/645129075'),
  ('Cancha de basquetbol junto al 3D',                                      'Deportes',                29.0818909, -110.9608631, 'OpenStreetMap way/645896530'),
  ('Edificio 3K3 - Matemáticas. Aulas',                                     'Edificio académico',      29.0811024, -110.9606020, 'OpenStreetMap way/662550754'),
  ('Caseta de alimentos junto al 3K3',                                      'Comida',                  29.0811295, -110.9603571, 'OpenStreetMap way/662550755'),
  ('Caseta de alimentos junto al 10J',                                      'Comida',                  29.0845746, -110.9625088, 'OpenStreetMap way/662550756'),
  ('Edificio 3A - Letras y Lingüística. Oficinas y Aulas',                  'Edificio académico',      29.0826671, -110.9601717, 'OpenStreetMap way/662550759'),
  ('Caseta de alimentos junto al 5L',                                       'Comida',                  29.0824528, -110.9628925, 'OpenStreetMap way/662554626'),
  ('Caseta de alimentos junto al 3V',                                       'Comida',                  29.0825559, -110.9609813, 'OpenStreetMap way/662593322'),
  ('Caseta de alimentos junto al 9K',                                       'Comida',                  29.0846899, -110.9642242, 'OpenStreetMap way/662593323'),
  ('Caseta de alimentos junto al 9G',                                       'Comida',                  29.0857404, -110.9644380, 'OpenStreetMap way/662593324'),
  ('Caseta de alimentos junto al 10C',                                      'Comida',                  29.0852924, -110.9634607, 'OpenStreetMap way/662593325'),
  ('Caseta de alimentos junto al 5H',                                       'Comida',                  29.0815770, -110.9634151, 'OpenStreetMap way/662593326'),
  ('Caseta de alimentos junto al 9P',                                       'Comida',                  29.0847323, -110.9657029, 'OpenStreetMap way/662720335'),
  ('Edificio 5C - Ingeniería Química y Metalurgia. Taller',                 'Laboratorio',             29.0809236, -110.9632250, 'OpenStreetMap way/664042933'),
  ('Radio Universidad 107.5 FM',                                            'Oficinas',                29.0848503, -110.9584914, 'OpenStreetMap way/664898209'),
  ('Plaza LCC',                                                             'Plazas y áreas verdes',   29.0806831, -110.9609060, 'OpenStreetMap way/675479174'),
  ('Campo de tiro con arco y atletismo (6H)',                               'Deportes',                29.0811333, -110.9655375, 'OpenStreetMap way/1126564948'),
  ('Estacionamiento 08-EM101',                                              'Estacionamiento',         29.0827824, -110.9658314, 'OpenStreetMap way/1126598495'),
  ('Edificio 6I - Rectoría. Caja de Ahorro',                                'Oficinas',                29.0821703, -110.9650218, 'OpenStreetMap way/1126632438'),
  ('Estacionamiento 03-EM103',                                              'Estacionamiento',         29.0817373, -110.9585182, 'OpenStreetMap way/1127374002'),
  ('Estacionamiento 03-EM102',                                              'Estacionamiento',         29.0814989, -110.9585357, 'OpenStreetMap way/1127374003'),
  ('Área verde biología',                                                   'Plazas y áreas verdes',   29.0812598, -110.9692371, 'OpenStreetMap way/1293748474'),
  ('Gimnasio Status Unison',                                                'Deportes',                29.0809448, -110.9652445, 'OpenStreetMap way/1300063191'),
  ('Edificio 6J - Taller de Muestras Ing. en Metalurgia',                   'Laboratorio',             29.0810976, -110.9637519, 'OpenStreetMap way/1300073531'),
  ('Estacionamiento 06-EM103',                                              'Estacionamiento',         29.0819862, -110.9652468, 'OpenStreetMap relation/15083305'),
  ('Edificio 3O - Arquitectura y Diseño Gráfico',                           'Edificio académico',      29.0815517, -110.9597049, 'OpenStreetMap relation/15090603')
) AS v(nombre, categoria, lat, lon, fuente)
JOIN categoria c ON c.nombre = v.categoria
WHERE NOT EXISTS (SELECT 1 FROM lugar l WHERE l.nombre = v.nombre);
"""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin cambios: los lugares pueden tener reseñas, favoritos o fotos de usuarios,
            // así que revertir la migración no los borra.
        }
    }
}
