using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public class GuardarLugarRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
    public Guid CategoriaId { get; set; }
    [Required, Range(-90d, 90d)] public double? Latitud { get; set; }
    [Required, Range(-180d, 180d)] public double? Longitud { get; set; }
    public short? Piso { get; set; }
    public bool Activo { get; set; } = true;
}

public class GuardarCategoriaRequest
{
    [Required, StringLength(100)] public string Nombre { get; set; } = null!;
    [StringLength(500)] public string? Icono { get; set; }
}

public record LugarAdminDto(Guid Id, string Nombre, string? Descripcion, Guid CategoriaId,
    string NombreCategoria, double Latitud, double Longitud, short? Piso, bool Activo,
    DateTime FechaCreacion, Guid? PropietarioId, string? EstadoSolicitud);
