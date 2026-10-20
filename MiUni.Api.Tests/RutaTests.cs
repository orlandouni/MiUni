using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using MiUni.Api.DTOs;
using MiUni.Api.Services;
using Npgsql;
using Xunit;

namespace MiUni.Api.Tests;

public sealed class RoutingFactAttribute : FactAttribute
{
    public RoutingFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION")))
            Skip = "Requiere MIUNI_ROUTING_TEST_CONNECTION con PostGIS y pgRouting.";
    }
}

public class RutaTests
{
    [Theory]
    [InlineData(null, -110d)]
    [InlineData(29d, null)]
    [InlineData(91d, -110d)]
    [InlineData(29d, -181d)]
    [InlineData(double.NaN, -110d)]
    public void RechazaCoordenadasInvalidas(double? latitud, double? longitud)
    {
        var punto = new PuntoRutaDto { Latitud = latitud, Longitud = longitud };
        Assert.False(Validator.TryValidateObject(punto, new ValidationContext(punto), [], true));
    }

    [Fact]
    public void RequiereAmbosPuntos()
    {
        var request = new RutaRequest();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [RoutingFact]
    public async Task DijkstraSeleccionaRutaCortaYOrientaGeometriaEnAmbosSentidos()
    {
        var forward = await Consultar(0, 0.002);
        var reverse = await Consultar(0.002, 0);
        Assert.NotNull(forward);
        Assert.NotNull(reverse);
        Assert.InRange(forward.Value.distancia, 220, 225);
        Assert.Equal(forward.Value.distancia, reverse.Value.distancia, 6);
        Assert.Equal(0, forward.Value.geo.GetProperty("coordinates")[0][0].GetDouble());
        Assert.Equal(0.002, reverse.Value.geo.GetProperty("coordinates")[0][0].GetDouble());
    }

    [RoutingFact]
    public async Task DevuelveCeroParaElMismoNodo()
    {
        var ruta = await Consultar(0, 0);
        Assert.NotNull(ruta);
        Assert.Equal(0, ruta.Value.distancia);
        Assert.Equal("Point", ruta.Value.geo.GetProperty("type").GetString());
    }

    [RoutingFact]
    public async Task RechazaRedDesconectadaPuntosLejanosYRedVacia()
    {
        Assert.Null(await Consultar(0, 0.01));
        Assert.Null(await Consultar(0, 1));
        Assert.Null(await Consultar(0, 0.002, vacia: true));
    }

    private static async Task<(double distancia, JsonElement geo)?> Consultar(double a, double b, bool vacia = false)
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("MIUNI_ROUTING_TEST_CONNECTION"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Sólo tablas temporales: nunca modifica la red real del usuario.
        const string fixture = """
            CREATE TEMP TABLE camino(id uuid DEFAULT gen_random_uuid(), geometria geography(LineString,4326), transitable boolean) ON COMMIT DROP;
            INSERT INTO camino(geometria, transitable) VALUES
              ('LINESTRING(0 0,0.001 0)', true),
              ('LINESTRING(0.002 0,0.001 0)', true),
              ('LINESTRING(0 0,0.001 0.001,0.002 0)', true),
              ('LINESTRING(0.01 0,0.011 0)', true),
              ('LINESTRING(0.002 0,0.01 0)', false);
            """;
        await using (var setup = new NpgsqlCommand(fixture, connection, transaction))
            await setup.ExecuteNonQueryAsync();
        if (vacia)
        {
            await using var empty = new NpgsqlCommand("TRUNCATE pg_temp.camino", connection, transaction);
            await empty.ExecuteNonQueryAsync();
        }

        // Ejecuta el SQL de producción con una fuente temporal aislada.
        static string Sql(string name) => (string)typeof(RutaService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        await using (var graph = new NpgsqlCommand(Sql("CrearGrafo").Replace("public.camino", "pg_temp.camino"), connection, transaction))
            await graph.ExecuteNonQueryAsync();
        await using var query = new NpgsqlCommand(Sql("CalcularRuta"), connection, transaction);
        query.Parameters.AddWithValue("lonA", a);
        query.Parameters.AddWithValue("lonB", b);
        query.Parameters.AddWithValue("latA", 0d);
        query.Parameters.AddWithValue("latB", 0d);
        query.Parameters.AddWithValue("radio", 100d);
        await using var reader = await query.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        using var json = JsonDocument.Parse(reader.GetString(1));
        return (reader.GetDouble(0), json.RootElement.Clone());
    }
}
