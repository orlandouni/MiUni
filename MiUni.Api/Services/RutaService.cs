using System.Text.Json;
using MiUni.Api.DTOs;
using Npgsql;

namespace MiUni.Api.Services;

public sealed class RutaService(NpgsqlDataSource dataSource) : IRutaService
{
    // Una instantánea por solicitud mantiene los identificadores estables durante Dijkstra.
    // Los cruces deben estar segmentados: sólo los extremos idénticos conectan caminos
    // (el importador de OpenStreetMap ya corta cada vía en sus intersecciones).
    private const string CrearGrafo = """
        CREATE TEMP TABLE ruta_aristas ON COMMIT DROP AS
        WITH caminos AS (
            SELECT id, geometria::geometry AS geom
            FROM public.camino
            WHERE transitable AND NOT ST_IsEmpty(geometria::geometry)
              AND ST_Length(geometria) > 0
        ), puntos AS (
            SELECT ST_AsEWKB(ST_StartPoint(geom)) AS clave FROM caminos
            UNION
            SELECT ST_AsEWKB(ST_EndPoint(geom)) FROM caminos
        ), vertices AS (
            SELECT row_number() OVER (ORDER BY clave) AS id, clave FROM puntos
        )
        SELECT row_number() OVER (ORDER BY c.id) AS id,
               a.id AS source, b.id AS target,
               ST_Length(c.geom::geography) AS cost, c.geom
        FROM caminos c
        JOIN vertices a ON a.clave = ST_AsEWKB(ST_StartPoint(c.geom))
        JOIN vertices b ON b.clave = ST_AsEWKB(ST_EndPoint(c.geom));
        """;

    // Proyecta A (vértice -1) y B (vértice -2) sobre el tramo más cercano y lo parte ahí,
    // así la ruta empieza y termina a media cuadra y no en la intersección más próxima.
    private const string ConectarPuntos = """
        CREATE TEMP TABLE ruta_puntos ON COMMIT DROP AS
        SELECT p.id, e.id AS arista,
               ST_LineLocatePoint(e.geom, p.pt) AS fraccion,
               ST_ClosestPoint(e.geom, p.pt) AS geom,
               e.distancia
        FROM (VALUES (-1, ST_SetSRID(ST_MakePoint(@lonA, @latA), 4326)),
                     (-2, ST_SetSRID(ST_MakePoint(@lonB, @latB), 4326))) AS p(id, pt)
        CROSS JOIN LATERAL (
            SELECT a.id, a.geom, ST_Distance(a.geom::geography, p.pt::geography) AS distancia
            FROM ruta_aristas a
            ORDER BY distancia, a.id
            LIMIT 1
        ) e
        WHERE e.distancia <= @radio;

        CREATE TEMP TABLE ruta_grafo ON COMMIT DROP AS
        WITH cortes AS (
            SELECT a.id AS arista, 0::double precision AS fraccion, a.source AS vertice FROM ruta_aristas a
            WHERE a.id IN (SELECT arista FROM ruta_puntos)
            UNION ALL
            SELECT a.id, 1, a.target FROM ruta_aristas a
            WHERE a.id IN (SELECT arista FROM ruta_puntos)
            UNION ALL
            SELECT arista, fraccion, id FROM ruta_puntos
        ), tramos AS (
            SELECT arista, fraccion AS desde, vertice AS source,
                   lead(fraccion) OVER w AS hasta, lead(vertice) OVER w AS target
            FROM cortes
            WINDOW w AS (PARTITION BY arista ORDER BY fraccion, vertice = (SELECT target FROM ruta_aristas WHERE id = arista), vertice)
        )
        SELECT id, source, target, cost, geom FROM ruta_aristas
        WHERE id NOT IN (SELECT arista FROM ruta_puntos)
        UNION ALL
        SELECT (SELECT max(id) FROM ruta_aristas) + row_number() OVER (ORDER BY t.arista, t.desde, t.source),
               t.source, t.target, (t.hasta - t.desde) * a.cost,
               ST_LineSubstring(a.geom, t.desde, t.hasta)
        FROM tramos t JOIN ruta_aristas a ON a.id = t.arista
        WHERE t.target IS NOT NULL;
        """;

    private const string CalcularRuta = """
        WITH extremos AS (
            SELECT a.geom AS ga, b.geom AS gb, a.distancia AS da, b.distancia AS db
            FROM ruta_puntos a JOIN ruta_puntos b ON a.id = -1 AND b.id = -2
        ), pasos AS (
            SELECT p.path_seq, p.cost,
                CASE WHEN p.node = e.source THEN e.geom ELSE ST_Reverse(e.geom) END AS geom
            FROM extremos x
            CROSS JOIN LATERAL pgr_dijkstra(
                'SELECT id, source, target, cost FROM pg_temp.ruta_grafo',
                -1, -2, directed := false) p
            JOIN ruta_grafo e ON e.id = p.edge
            WHERE p.edge <> -1
        ), total AS (
            SELECT COALESCE(sum(cost), 0)::double precision AS metros FROM pasos
        )
        SELECT total.metros,
            ST_AsGeoJSON(CASE WHEN total.metros = 0 THEN ga
                ELSE (SELECT ST_RemoveRepeatedPoints(ST_MakeLine(geom ORDER BY path_seq)) FROM pasos) END),
            ST_Y(ga), ST_X(ga), ST_Y(gb), ST_X(gb), da, db
        FROM extremos CROSS JOIN total
        WHERE EXISTS (SELECT 1 FROM pasos);
        """;

    public async Task<RutaResponse?> CalcularAsync(RutaRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var graph = new NpgsqlCommand(CrearGrafo, connection, transaction))
            await graph.ExecuteNonQueryAsync(cancellationToken);

        await using (var connect = new NpgsqlCommand(ConectarPuntos, connection, transaction))
        {
            connect.Parameters.AddWithValue("lonA", request.A.Longitud!.Value);
            connect.Parameters.AddWithValue("latA", request.A.Latitud!.Value);
            connect.Parameters.AddWithValue("lonB", request.B.Longitud!.Value);
            connect.Parameters.AddWithValue("latB", request.B.Latitud!.Value);
            connect.Parameters.AddWithValue("radio", request.RadioConexionMetros);
            await connect.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(CalcularRuta, connection, transaction);

        RutaResponse? result = null;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                using var json = JsonDocument.Parse(reader.GetString(1));
                result = new RutaResponse(reader.GetDouble(0), json.RootElement.Clone(),
                    new PuntoRutaDto { Latitud = reader.GetDouble(2), Longitud = reader.GetDouble(3) },
                    new PuntoRutaDto { Latitud = reader.GetDouble(4), Longitud = reader.GetDouble(5) },
                    reader.GetDouble(6), reader.GetDouble(7));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
