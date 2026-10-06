using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public class SolicitarNegocioRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
    public Guid CategoriaId { get; set; }
    [Required, Range(-90d, 90d)] public double? Latitud { get; set; }
    [Required, Range(-180d, 180d)] public double? Longitud { get; set; }
}

public class EditarNegocioRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
}

public record NegocioDto(Guid Id, string Nombre, string? Descripcion, Guid? PropietarioId,
    string? EstadoSolicitud, double Latitud, double Longitud);
