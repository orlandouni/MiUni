using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MiUni.Api.DTOs;

public sealed class PuntoRutaDto
{
    [Required, Range(-90d, 90d)]
    public double? Latitud { get; set; }

    [Required, Range(-180d, 180d)]
    public double? Longitud { get; set; }
}

public sealed class RutaRequest
{
    [Required]
    public PuntoRutaDto A { get; set; } = null!;

    [Required]
    public PuntoRutaDto B { get; set; } = null!;

    // Evita conectar puntos fuera del campus a una red arbitrariamente lejana.
    [Range(1d, 1000d)]
    public double RadioConexionMetros { get; set; } = 100;
}

public sealed record RutaResponse(
    double DistanciaMetros,
    JsonElement Geometria,
    PuntoRutaDto OrigenConectado,
    PuntoRutaDto DestinoConectado,
    double DistanciaConexionAMetros,
    double DistanciaConexionBMetros);
