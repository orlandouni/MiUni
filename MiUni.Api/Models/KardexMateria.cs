using MiUni.Api.Enums;
using MiUni.Api.Identity;

namespace MiUni.Api.Models;

// Una materia del kárdex del estudiante: lo que cursó, cursa o dio de baja.
public class KardexMateria
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Materia { get; set; } = null!;

    // Clave oficial de la materia, si el estudiante la conoce.
    public string? Clave { get; set; }

    // Periodo escolar, por ejemplo "2026-2".
    public string? Periodo { get; set; }

    // Semestre de la carrera en el que se cursó (1 a 12).
    public short? Semestre { get; set; }

    // Calificación final de 0 a 100; vacía mientras se cursa.
    public decimal? Calificacion { get; set; }

    public short Creditos { get; set; }

    public EstadoMateria Estado { get; set; }

    public virtual ApplicationUser Usuario { get; set; } = null!;
}
