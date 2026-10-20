using System.ComponentModel.DataAnnotations;
using MiUni.Api.Enums;

namespace MiUni.Api.DTOs;

public class CrearReporteRequest : IValidatableObject
{
    public Guid? LugarId { get; set; }
    public Guid? ResenaId { get; set; }
    [Required, StringLength(2000)] public string Motivo { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LugarId.HasValue == ResenaId.HasValue)
            yield return new ValidationResult("Indica exactamente uno: LugarId o ResenaId.", [nameof(LugarId), nameof(ResenaId)]);
        if (LugarId == Guid.Empty || ResenaId == Guid.Empty)
            yield return new ValidationResult("El identificador no puede estar vacío.", [nameof(LugarId), nameof(ResenaId)]);
    }
}

public class EditarReporteRequest
{
    [Required, StringLength(2000)] public string Motivo { get; set; } = null!;
}

public class EstadoReporteRequest
{
    [Required, EnumDataType(typeof(EstadoReporte))] public EstadoReporte? Estado { get; set; }
}

public record ReporteUsuarioDto(Guid Id, Guid UsuarioId, string NombreUsuario, string? EmailUsuario,
    Guid? LugarId, Guid? ResenaId, Guid? LugarReportadoId, string? NombreLugar,
    string? ComentarioResena, string Motivo, EstadoReporte Estado, DateTime? FechaCreacion);

public record PaginaReportesDto(IReadOnlyList<ReporteUsuarioDto> Items, int Total, int Pagina, int TamanoPagina);
public record ResumenReporteDto(EstadoReporte Estado, int Cantidad);
