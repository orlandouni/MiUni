using MiUni.Api.Identity;

namespace MiUni.Api.Models;

// Una clase del horario personal de un estudiante (una fila por día y bloque de horas).
public class HorarioClase
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Materia { get; set; } = null!;

    public string? Profesor { get; set; }

    // 1 = lunes ... 7 = domingo (ISO 8601).
    public short Dia { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public string? Salon { get; set; }

    public virtual ApplicationUser Usuario { get; set; } = null!;
}
