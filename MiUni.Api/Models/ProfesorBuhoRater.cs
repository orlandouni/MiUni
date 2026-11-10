using MiUni.Api.Identity;

namespace MiUni.Api.Models;

// Perfil de BuhoRater de un profesor, compartido por todos los estudiantes: basta con que
// uno pegue el enlace para que el botón lleve directo al perfil en el horario de todos.
public class ProfesorBuhoRater
{
    // Nombre normalizado (mayúsculas, sin acentos ni espacios repetidos).
    public string Clave { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public Guid PerfilId { get; set; }

    public Guid? ActualizadoPorId { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public virtual ApplicationUser? ActualizadoPor { get; set; }
}
