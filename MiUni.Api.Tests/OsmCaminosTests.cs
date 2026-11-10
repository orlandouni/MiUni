using System.Text.Json;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class OsmCaminosTests
{
    // Cruz en el nodo 2 (andador horizontal + calle vertical), una autopista, un andador
    // prohibido a pie, un estacionamiento privado y un andador aislado.
    private const string Overpass = """
        {"elements":[
          {"type":"way","id":1,"tags":{"highway":"footway","surface":"concrete"},"nodes":[1,2,3],
           "geometry":[{"lat":0,"lon":0},{"lat":0,"lon":0.001},{"lat":0,"lon":0.002}]},
          {"type":"way","id":2,"tags":{"highway":"residential"},"nodes":[4,2,5],
           "geometry":[{"lat":-0.001,"lon":0.001},{"lat":0,"lon":0.001},{"lat":0.001,"lon":0.001}]},
          {"type":"way","id":3,"tags":{"highway":"motorway"},"nodes":[3,6],
           "geometry":[{"lat":0,"lon":0.002},{"lat":0,"lon":0.004}]},
          {"type":"way","id":4,"tags":{"highway":"footway","foot":"no"},"nodes":[5,7],
           "geometry":[{"lat":0.001,"lon":0.001},{"lat":0.002,"lon":0.001}]},
          {"type":"way","id":5,"tags":{"highway":"service","access":"private"},"nodes":[1,8],
           "geometry":[{"lat":0,"lon":0},{"lat":-0.001,"lon":0}]},
          {"type":"way","id":6,"tags":{"highway":"footway"},"nodes":[10,11],
           "geometry":[{"lat":0.01,"lon":0.01},{"lat":0.01,"lon":0.0101}]},
          {"type":"node","id":1,"lat":0,"lon":0}
        ]}
        """;

    [Fact]
    public void CortaEnInterseccionesYDescartaViasNoCaminablesEIslas()
    {
        using var doc = JsonDocument.Parse(Overpass);
        var r = OsmCaminos.Segmentar(doc.RootElement);

        Assert.Equal(4, r.Tramos.Count);
        Assert.Equal(2, r.ViasUsadas);
        Assert.Equal(1, r.TramosAislados);
        Assert.All(r.Tramos, t => Assert.Equal(2, t.Puntos.Count));
        Assert.Contains(r.Tramos, t => OsmCaminos.Wkt(t) == "LINESTRING(0 0,0.001 0)" && t.Superficie == "concrete");
        Assert.Contains(r.Tramos, t => OsmCaminos.Wkt(t) == "LINESTRING(0.001 0,0.001 0.001)" && t.WayId == 2);
    }

    [Fact]
    public void IncluyePrivadosSoloSiSePide()
    {
        using var doc = JsonDocument.Parse(Overpass);
        var r = OsmCaminos.Segmentar(doc.RootElement, incluirPrivados: true);
        Assert.Equal(5, r.Tramos.Count);
        Assert.Contains(r.Tramos, t => t.WayId == 5);
    }

    [Theory]
    [InlineData("footway", null, null, true)]
    [InlineData("motorway", null, null, false)]
    [InlineData("primary", "no", null, false)]
    [InlineData("service", null, "private", false)]
    [InlineData("service", "yes", "private", true)]
    [InlineData("track", null, "no", false)]
    public void ClasificaViasCaminables(string highway, string? foot, string? access, bool esperado)
    {
        var tags = new Dictionary<string, string> { ["highway"] = highway };
        if (foot is not null) tags["foot"] = foot;
        if (access is not null) tags["access"] = access;
        Assert.Equal(esperado, OsmCaminos.EsCaminable(tags));
    }

    [Fact]
    public void InterpretaAreas()
    {
        Assert.Equal(AreaOsm.Campus, AreaOsm.Parse("campus"));
        Assert.Equal(new AreaOsm(29.07, -110.98, 29.09, -110.95), AreaOsm.Parse("29.07, -110.98, 29.09, -110.95"));
        Assert.Throws<ArgumentException>(() => AreaOsm.Parse("29.09,-110.98,29.07,-110.95"));
        Assert.Throws<ArgumentException>(() => AreaOsm.Parse("0,0,1,1"));
        Assert.Contains("(29.0755,-110.975,29.0915,-110.9535)", AreaOsm.Campus.ConsultaOverpass());
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> Hosts { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Hosts.Add(request.RequestUri!.Host);
            return Task.FromResult(responder(request));
        }
    }

    [Fact]
    public async Task ReintentaYPasaAlSiguienteServidorSiOverpassFalla()
    {
        var handler = new FakeHandler(r => r.RequestUri!.Host == "uno.test"
            ? new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway)
            : new HttpResponseMessage { Content = new StringContent(Overpass) });
        using var http = new HttpClient(handler);
        using var doc = await OsmCaminos.DescargarAsync(http, AreaOsm.Campus,
            ["https://uno.test/api/interpreter", "https://dos.test/api/interpreter"], espera: TimeSpan.Zero);
        Assert.Equal(["uno.test", "uno.test", "dos.test"], handler.Hosts);
        Assert.Equal(7, doc.RootElement.GetProperty("elements").GetArrayLength());
    }

    [Fact]
    public async Task FallaConMensajeClaroSiNingunServidorResponde()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage
            { Content = new StringContent("""{"elements":[],"remark":"runtime error: Query timed out"}""") });
        using var http = new HttpClient(handler);
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => OsmCaminos.DescargarAsync(http, AreaOsm.Campus,
            ["https://uno.test/x"], espera: TimeSpan.Zero));
        Assert.Contains("Query timed out", ex.Message);
        Assert.Equal(2, handler.Hosts.Count);
    }
}
