using System.ComponentModel.DataAnnotations;
using MiUni.Api.Enums;

namespace MiUni.Api.DTOs;

public record KardexMateriaDto(Guid Id, string Materia, string? Clave, string? Periodo, short? Semestre,
    decimal? Calificacion, short Creditos, EstadoMateria Estado);

// Promedio de las materias con calificación final (aprobadas y reprobadas).
public record KardexResumenDto(int CreditosAprobados, decimal? Promedio, int MateriasAprobadas,
    int MateriasReprobadas, int MateriasCursando);

public record KardexDto(IReadOnlyList<KardexMateriaDto> Materias, KardexResumenDto Resumen);

public class GuardarKardexMateriaRequest
{
    [Required(ErrorMessage = "Escribe el nombre de la materia.")]
    [StringLength(120, ErrorMessage = "La materia no puede pasar de 120 caracteres.")]
    public string Materia { get; set; } = "";

    [StringLength(20, ErrorMessage = "La clave no puede pasar de 20 caracteres.")]
    public string? Clave { get; set; }

    [StringLength(20, ErrorMessage = "El periodo no puede pasar de 20 caracteres.")]
    public string? Periodo { get; set; }

    [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
    public short? Semestre { get; set; }

    [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100.")]
    public decimal? Calificacion { get; set; }

    [Range(0, 30, ErrorMessage = "Los créditos deben estar entre 0 y 30.")]
    public short Creditos { get; set; }

    [EnumDataType(typeof(EstadoMateria), ErrorMessage = "El estado de la materia no es válido.")]
    public EstadoMateria Estado { get; set; }
}
