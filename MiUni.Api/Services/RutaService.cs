using System.Text.Json;
using MiUni.Api.DTOs;
using Npgsql;

namespace MiUni.Api.Services;

public sealed class RutaService(NpgsqlDataSource dataSource) : IRutaService
{
    // Una instantánea por solicitud mantiene los identificadores estables durante Dijkstra.
    // Los cruces deben estar segmentados: sólo los extremos idénticos conectan caminos.
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

        CREATE TEMP TABLE ruta_vertices ON COMMIT DROP AS
        SELECT source AS id, ST_StartPoint(geom) AS geom FROM ruta_aristas
        UNION
        SELECT target, ST_EndPoint(geom) FROM ruta_aristas;
        """;

    private const string CalcularRuta = """
        WITH a AS (
            SELECT id, geom,
                ST_Distance(geom::geography, ST_SetSRID(ST_MakePoint(@lonA, @latA), 4326)::geography) AS distancia
            FROM ruta_vertices
            ORDER BY distancia, id LIMIT 1
        ), b AS (
            SELECT id, geom,
                ST_Distance(geom::geography, ST_SetSRID(ST_MakePoint(@lonB, @latB), 4326)::geography) AS distancia
            FROM ruta_vertices
            ORDER BY distancia, id LIMIT 1
        ), extremos AS (
            SELECT a.id AS inicio, b.id AS fin, a.geom AS ga, b.geom AS gb,
                   a.distancia AS da, b.distancia AS db
            FROM a CROSS JOIN b
            WHERE a.distancia <= @radio AND b.distancia <= @radio
        ), pasos AS (
            SELECT p.path_seq, p.cost,
                CASE WHEN p.node = e.source THEN e.geom ELSE ST_Reverse(e.geom) END AS geom
            FROM extremos x
            CROSS JOIN LATERAL pgr_dijkstra(
                'SELECT id, source, target, cost FROM pg_temp.ruta_aristas',
                x.inicio, x.fin, directed := false) p
            JOIN ruta_aristas e ON e.id = p.edge
            WHERE p.edge <> -1
        )
        SELECT COALESCE((SELECT sum(cost) FROM pasos), 0)::double precision,
            ST_AsGeoJSON(CASE WHEN inicio = fin THEN ga
                ELSE (SELECT ST_MakeLine(geom ORDER BY path_seq) FROM pasos) END),
            ST_Y(ga), ST_X(ga), ST_Y(gb), ST_X(gb), da, db
        FROM extremos
        WHERE inicio = fin OR EXISTS (SELECT 1 FROM pasos);
        """;

    public async Task<RutaResponse?> CalcularAsync(RutaRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var graph = new NpgsqlCommand(CrearGrafo, connection, transaction))
            await graph.ExecuteNonQueryAsync(cancellationToken);

        await using var command = new NpgsqlCommand(CalcularRuta, connection, transaction);
        command.Parameters.AddWithValue("lonA", request.A.Longitud!.Value);
        command.Parameters.AddWithValue("latA", request.A.Latitud!.Value);
        command.Parameters.AddWithValue("lonB", request.B.Longitud!.Value);
        command.Parameters.AddWithValue("latB", request.B.Latitud!.Value);
        command.Parameters.AddWithValue("radio", request.RadioConexionMetros);

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
