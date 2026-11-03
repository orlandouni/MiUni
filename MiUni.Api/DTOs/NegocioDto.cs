using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public class SolicitarNegocioRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
    public Guid CategoriaId { get; set; }
    // Para negocios ambulantes, la zona aproximada donde suelen vender.
    [Required, Range(-90d, 90d)] public double? Latitud { get; set; }
    [Required, Range(-180d, 180d)] public double? Longitud { get; set; }
    public bool EsAmbulante { get; set; }
}

// Los campos opcionales sólo se actualizan cuando se envían.
public class EditarNegocioRequest : IValidatableObject
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(2000)] public string? Descripcion { get; set; }
    public Guid? CategoriaId { get; set; }
    [Range(-90d, 90d)] public double? Latitud { get; set; }
    [Range(-180d, 180d)] public double? Longitud { get; set; }
    public bool? EsAmbulante { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitud.HasValue != Longitud.HasValue)
            yield return new ValidationResult("Envía latitud y longitud juntas.", [nameof(Latitud), nameof(Longitud)]);
    }
}

public record NegocioDto(Guid Id, string Nombre, string? Descripcion, Guid? PropietarioId,
    string? EstadoSolicitud, double Latitud, double Longitud, Guid CategoriaId, string? NombreCategoria,
    bool EsAmbulante, bool Activo, DateTime FechaCreacion, int Resenas, double? Promedio, int Favoritos);

public record HorarioDto(short DiaSemana, string HoraApertura, string HoraCierre);

public class HorarioRequest
{
    // 1 = lunes … 7 = domingo.
    [Range(1, 7)] public short DiaSemana { get; set; }
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Usa el formato HH:mm.")]
    public string HoraApertura { get; set; } = null!;
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Usa el formato HH:mm.")]
    public string HoraCierre { get; set; } = null!;
}

public class GuardarHorariosRequest
{
    [Required, MaxLength(28)] public List<HorarioRequest> Horarios { get; set; } = [];
}

public record ProductoDto(Guid Id, string Nombre, string? Descripcion, decimal Precio, bool EstaDisponible);

public class GuardarProductoRequest
{
    [Required, StringLength(150)] public string Nombre { get; set; } = null!;
    [StringLength(500)] public string? Descripcion { get; set; }
    [Range(typeof(decimal), "0", "100000")] public decimal Precio { get; set; }
    public bool EstaDisponible { get; set; } = true;
}

public record ResenaNegocioDto(Guid Id, string NombreUsuario, short Calificacion, string? Comentario, DateTime FechaCreacion);

public record PanelNegocioDto(NegocioDto Negocio, IEnumerable<HorarioDto> Horarios,
    IEnumerable<ProductoDto> Productos, IEnumerable<ResenaNegocioDto> Resenas);

public record InfoNegocioDto(bool EsAmbulante, IEnumerable<HorarioDto> Horarios, IEnumerable<ProductoDto> Productos);
