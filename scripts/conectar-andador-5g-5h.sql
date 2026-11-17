-- Correccion local: paso peatonal confirmado por Diego el 2026-11-17.
-- Trazado aproximado por el lado sur de 5G/5H hasta Calle del Conocimiento.
-- No modifica OpenStreetMap. Aplicar con psql -v ON_ERROR_STOP=1 -f este_archivo.
-- Respaldar camino antes de ejecutar. Reaplicar despues de importar OSM.
BEGIN;
SELECT pg_advisory_xact_lock(517517);
LOCK TABLE public.camino IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
  inicio geometry;
  llegada geometry;
BEGIN
  SELECT ST_StartPoint(geometria::geometry) INTO inicio FROM public.camino
  WHERE transitable AND osm_way_id = 652140268
    AND ST_DWithin(ST_StartPoint(geometria::geometry)::geography,
        ST_SetSRID(ST_MakePoint(-110.962561,29.0815731),4326)::geography, 1)
  LIMIT 1;
  SELECT ST_ClosestPoint(geometria::geometry,
      ST_SetSRID(ST_MakePoint(-110.96355,29.081455),4326)) INTO llegada
  FROM public.camino WHERE transitable AND osm_way_id = 424687769
  ORDER BY ST_Distance(geometria, ST_SetSRID(ST_MakePoint(-110.96355,29.081455),4326)::geography)
  LIMIT 1;
  IF inicio IS NULL OR llegada IS NULL THEN
    RAISE EXCEPTION 'No se encontraron los accesos OSM esperados. Revisar antes de aplicar.';
  END IF;
  INSERT INTO public.camino(id,geometria,transitable,osm_way_id)
  VALUES ('51751700-0000-4000-8000-000000000001',
    ST_MakeLine(ARRAY[inicio,
      ST_SetSRID(ST_MakePoint(-110.962945,29.081521),4326),
      ST_SetSRID(ST_MakePoint(-110.96335,29.081475),4326),llegada])::geography,
    true,NULL)
  ON CONFLICT (id) DO NOTHING;
END $$;

-- Conservar los originales; partir unicamente caminos que intersectan el acceso.
CREATE TEMP TABLE ajuste_caminos ON COMMIT DROP AS
SELECT c.id,c.osm_way_id,c.tipo_superficie,c.geometria::geometry AS geom
FROM public.camino c
WHERE c.transitable AND ST_Intersects(c.geometria::geometry,
  (SELECT geometria::geometry FROM public.camino WHERE id='51751700-0000-4000-8000-000000000001'));

CREATE TEMP TABLE ajuste_cruces ON COMMIT DROP AS
SELECT a.id AS a,b.id AS b,(ST_Dump(ST_CollectionExtract(ST_Intersection(a.geom,b.geom),1))).geom AS punto
FROM ajuste_caminos a JOIN ajuste_caminos b ON a.id < b.id AND ST_Intersects(a.geom,b.geom);

CREATE TEMP TABLE ajuste_cortes ON COMMIT DROP AS
WITH puntos AS (
 SELECT a AS id,punto FROM ajuste_cruces UNION SELECT b,punto FROM ajuste_cruces
), cortes AS (
 SELECT c.id,ST_LineLocatePoint(c.geom,p.punto) AS f,p.punto
 FROM ajuste_caminos c JOIN puntos p USING(id)
)
SELECT DISTINCT ON(id,f) id,f,punto FROM cortes
WHERE f>1e-10 AND f<1-1e-10 ORDER BY id,f,ST_AsEWKB(punto);

CREATE TEMP TABLE ajuste_partes ON COMMIT DROP AS
WITH puntos AS (
 SELECT id,f,punto FROM ajuste_cortes
 UNION ALL SELECT id,0,ST_StartPoint(geom) FROM ajuste_caminos WHERE id IN(SELECT id FROM ajuste_cortes)
 UNION ALL SELECT id,1,ST_EndPoint(geom) FROM ajuste_caminos WHERE id IN(SELECT id FROM ajuste_cortes)
), tramos AS (
 SELECT id,f,punto,lead(f) OVER w AS fin,lead(punto) OVER w AS siguiente
 FROM puntos WINDOW w AS(PARTITION BY id ORDER BY f)
)
SELECT c.id AS original,c.osm_way_id,c.tipo_superficie,
 ST_SetPoint(ST_SetPoint(ST_LineSubstring(c.geom,t.f,t.fin),0,t.punto),-1,t.siguiente)::geography AS geometria
FROM tramos t JOIN ajuste_caminos c USING(id) WHERE t.fin>t.f;

UPDATE public.camino SET transitable=false WHERE id IN(SELECT original FROM ajuste_partes);
INSERT INTO public.camino(geometria,transitable,tipo_superficie,osm_way_id)
SELECT geometria,true,tipo_superficie,osm_way_id FROM ajuste_partes;
COMMIT;
