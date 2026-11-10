using System.Globalization;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace MiUni.Api.Services;

public sealed record AreaOsm(double Sur, double Oeste, double Norte, double Este)
{
    // Campus de la Unison y sus alrededores (incluye los lugares de seed_lugares.sql con ~500 m de margen).
    public static readonly AreaOsm Campus = new(29.0755, -110.9750, 29.0915, -110.9535);

    public static AreaOsm Parse(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Equals("campus", StringComparison.OrdinalIgnoreCase)
            || valor.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            return Campus;
        var partes = valor.Split(',', StringSplitOptions.TrimEntries);
        if (partes.Length != 4 || !partes.All(p => double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            throw new ArgumentException("El área debe ser 'campus' o 'sur,oeste,norte,este' en grados decimales.");
        var n = partes.Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        if (n[0] >= n[2] || n[1] >= n[3] || n[0] < -90 || n[2] > 90 || n[1] < -180 || n[3] > 180)
            throw new ArgumentException("Área inválida: se espera sur < norte y oeste < este.");
        if ((n[2] - n[0]) * (n[3] - n[1]) > 0.01)
            throw new ArgumentException("Área demasiado grande (máximo ~0.01 grados², unos 10 km x 10 km).");
        return new AreaOsm(n[0], n[1], n[2], n[3]);
    }

    public string ConsultaOverpass() => string.Create(CultureInfo.InvariantCulture,
        $"[out:json][timeout:90];way[\"highway\"]({Sur},{Oeste},{Norte},{Este});out body geom;");
}

public sealed record TramoOsm(long WayId, IReadOnlyList<(double Lon, double Lat)> Puntos, string? Superficie);

public sealed record ResultadoSegmentacion(IReadOnlyList<TramoOsm> Tramos, int ViasUsadas, int TramosAislados);

/// <summary>
/// Convierte la respuesta de Overpass en tramos aptos para pgRouting: sólo vías por las que
/// se puede caminar, cortadas en cada nodo compartido y sin islas desconectadas.
/// </summary>
public static class OsmCaminos
{
    private static readonly HashSet<string> Peatonales =
    [
        "footway", "path", "pedestrian", "steps", "living_street", "residential", "service",
        "unclassified", "tertiary", "tertiary_link", "secondary", "secondary_link", "primary",
        "primary_link", "track", "cycleway", "corridor", "road",
    ];

    private static readonly HashSet<string> PieSi = ["yes", "designated", "permissive", "destination"];

    public static bool EsCaminable(IReadOnlyDictionary<string, string> tags, bool incluirPrivados = false)
    {
        if (!tags.TryGetValue("highway", out var highway) || !Peatonales.Contains(highway)) return false;
        tags.TryGetValue("foot", out var foot);
        if (foot is "no" or "use_sidepath") return false;
        if (foot is not null && PieSi.Contains(foot)) return true;
        if (tags.TryGetValue("access", out var access))
        {
            if (access == "no") return false;
            if (access == "private" && !incluirPrivados) return false;
        }
        return true;
    }

    public static ResultadoSegmentacion Segmentar(JsonElement overpass, bool incluirPrivados = false)
    {
        var vias = new List<(long Id, long[] Nodos, (double, double)[] Puntos, string? Superficie)>();
        foreach (var el in overpass.GetProperty("elements").EnumerateArray())
        {
            if (el.GetProperty("type").GetString() != "way") continue;
            var tags = new Dictionary<string, string>();
            if (el.TryGetProperty("tags", out var t))
                foreach (var p in t.EnumerateObject()) tags[p.Name] = p.Value.GetString() ?? "";
            if (!EsCaminable(tags, incluirPrivados)) continue;
            if (!el.TryGetProperty("nodes", out var nodesEl) || !el.TryGetProperty("geometry", out var geomEl)) continue;
            var nodos = nodesEl.EnumerateArray().Select(n => n.GetInt64()).ToArray();
            var puntos = geomEl.EnumerateArray()
                .Select(g => (g.GetProperty("lon").GetDouble(), g.GetProperty("lat").GetDouble())).ToArray();
            if (nodos.Length < 2 || nodos.Length != puntos.Length) continue;
            vias.Add((el.GetProperty("id").GetInt64(), nodos, puntos, tags.GetValueOrDefault("surface")));
        }

        // Un nodo usado más de una vez (por otra vía o por la misma) es una intersección.
        var usos = new Dictionary<long, int>();
        foreach (var v in vias)
            foreach (var n in v.Nodos) usos[n] = usos.GetValueOrDefault(n) + 1;

        var tramos = new List<(TramoOsm Tramo, long Inicio, long Fin)>();
        foreach (var v in vias)
        {
            var desde = 0;
            for (var i = 1; i < v.Nodos.Length; i++)
            {
                if (i != v.Nodos.Length - 1 && usos[v.Nodos[i]] < 2) continue;
                var puntos = v.Puntos[desde..(i + 1)];
                if (puntos.Distinct().Count() >= 2)
                    tramos.Add((new TramoOsm(v.Id, puntos, v.Superficie), v.Nodos[desde], v.Nodos[i]));
                desde = i;
            }
        }

        // Se conserva sólo la red conectada más grande: un andador suelto dentro de un edificio
        // atraparía al punto más cercano y la API respondería que no hay ruta.
        var padre = new Dictionary<long, long>();
        long Raiz(long x)
        {
            while (padre[x] != x) x = padre[x] = padre[padre[x]];
            return x;
        }
        foreach (var (_, a, b) in tramos)
        {
            padre.TryAdd(a, a); padre.TryAdd(b, b);
            var (ra, rb) = (Raiz(a), Raiz(b));
            if (ra != rb) padre[ra] = rb;
        }
        var principal = tramos.GroupBy(t => Raiz(t.Inicio))
            .OrderByDescending(g => g.Sum(t => Longitud(t.Tramo.Puntos))).FirstOrDefault()?.Key;
        var conectados = tramos.Where(t => Raiz(t.Inicio) == principal).Select(t => t.Tramo).ToList();
        return new ResultadoSegmentacion(conectados, conectados.Select(t => t.WayId).Distinct().Count(),
            tramos.Count - conectados.Count);
    }

    public static string Wkt(TramoOsm tramo)
    {
        var sb = new StringBuilder("LINESTRING(");
        for (var i = 0; i < tramo.Puntos.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(tramo.Puntos[i].Lon.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(tramo.Puntos[i].Lat.ToString("R", CultureInfo.InvariantCulture));
        }
        return sb.Append(')').ToString();
    }

    private static double Longitud(IReadOnlyList<(double Lon, double Lat)> p)
    {
        double total = 0;
        for (var i = 1; i < p.Count; i++)
        {
            var dx = (p[i].Lon - p[i - 1].Lon) * Math.Cos(p[i].Lat * Math.PI / 180);
            var dy = p[i].Lat - p[i - 1].Lat;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        return total;
    }

    /// <summary>Reemplaza en una transacción los caminos importados antes; los manuales no se tocan.</summary>
    public static async Task<int> GuardarAsync(NpgsqlDataSource dataSource, IReadOnlyList<TramoOsm> tramos, CancellationToken ct = default)
    {
        if (tramos.Count == 0) throw new InvalidOperationException("OpenStreetMap no devolvió caminos peatonales en el área; no se modificó la red.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var delete = new NpgsqlCommand("DELETE FROM public.camino WHERE osm_way_id IS NOT NULL", connection, transaction))
            await delete.ExecuteNonQueryAsync(ct);
        const string insert = """
            INSERT INTO public.camino (geometria, transitable, tipo_superficie, osm_way_id)
            SELECT ST_GeomFromText(t.wkt, 4326)::geography, true, t.superficie, t.way
            FROM unnest(@wkt, @superficie, @way) AS t(wkt, superficie, way)
            """;
        await using (var command = new NpgsqlCommand(insert, connection, transaction))
        {
            command.Parameters.AddWithValue("wkt", tramos.Select(Wkt).ToArray());
            command.Parameters.AddWithValue("superficie", tramos.Select(t => t.Superficie).ToArray());
            command.Parameters.AddWithValue("way", tramos.Select(t => t.WayId).ToArray());
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return tramos.Count;
    }

    public static async Task<long> ContarManualesAsync(NpgsqlDataSource dataSource, CancellationToken ct = default)
    {
        await using var command = dataSource.CreateCommand("SELECT count(*) FROM public.camino WHERE osm_way_id IS NULL");
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    public static readonly string[] ServidoresOverpass =
    [
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
    ];

    /// <summary>
    /// Prueba cada servidor hasta dos veces (los públicos se saturan seguido: 429, 502, 504)
    /// y pasa al siguiente si falla. Sólo lanza error si todos fallan.
    /// </summary>
    public static async Task<JsonDocument> DescargarAsync(HttpClient http, AreaOsm area, IEnumerable<string> servidores,
        Action<string>? log = null, TimeSpan? espera = null, CancellationToken ct = default)
    {
        var fallos = new List<string>();
        foreach (var url in servidores)
        {
            for (var intento = 1; intento <= 2; intento++)
            {
                try
                {
                    using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("data", area.ConsultaOverpass())]);
                    using var response = await http.PostAsync(url, content, ct);
                    if (!response.IsSuccessStatusCode) throw new HttpRequestException($"respondió {(int)response.StatusCode}");
                    await using var stream = await response.Content.ReadAsStreamAsync(ct);
                    var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                    // Overpass a veces responde 200 con un "remark" de error y sin elementos.
                    if (doc.RootElement.TryGetProperty("remark", out var remark)
                        && (!doc.RootElement.TryGetProperty("elements", out var els) || els.GetArrayLength() == 0))
                    {
                        var detalle = remark.GetString();
                        doc.Dispose();
                        throw new HttpRequestException($"devolvió un error: {detalle}");
                    }
                    return doc;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
                {
                    var motivo = ex is TaskCanceledException ? "tardó demasiado" : ex.Message;
                    fallos.Add($"{new Uri(url).Host} {motivo}");
                    log?.Invoke($"{new Uri(url).Host} {motivo} (intento {intento} de 2).");
                    if (intento == 1) await Task.Delay(espera ?? TimeSpan.FromSeconds(5), ct);
                }
            }
        }
        throw new HttpRequestException("Ningún servidor de OpenStreetMap respondió: " + string.Join("; ", fallos)
            + ". Espera unos minutos y vuelve a intentarlo, o indica otro con --osm-overpass-url.");
    }
}
