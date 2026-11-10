using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public record HorarioClaseDto(Guid Id, string Materia, string? Profesor, short Dia,
    TimeOnly HoraInicio, TimeOnly HoraFin, string? Salon, string? BuhoRaterUrl);

public class GuardarHorarioClaseRequest
{
    [Required(ErrorMessage = "Escribe el nombre de la materia.")]
    [StringLength(120, ErrorMessage = "La materia no puede pasar de 120 caracteres.")]
    public string Materia { get; set; } = "";

    [StringLength(120, ErrorMessage = "El nombre del profesor no puede pasar de 120 caracteres.")]
    public string? Profesor { get; set; }

    [Range(1, 7, ErrorMessage = "El día debe estar entre 1 (lunes) y 7 (domingo).")]
    public short Dia { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    [StringLength(60, ErrorMessage = "El salón no puede pasar de 60 caracteres.")]
    public string? Salon { get; set; }
}
