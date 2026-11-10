using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public record HorarioClaseDto(Guid Id, string Materia, string? Profesor, short Dia,
    TimeOnly HoraInicio, TimeOnly HoraFin, string? Salon, string? BuhoRaterUrl, bool BuhoRaterPerfil);

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

public record ClaseImportadaDto(string Materia, string? Profesor, short Dia,
    TimeOnly HoraInicio, TimeOnly HoraFin, string? Salon, string? BuhoRaterUrl, bool BuhoRaterPerfil);

public record PerfilBuhoRaterDto(string Profesor, string? BuhoRaterUrl, bool BuhoRaterPerfil);

public class VincularBuhoRaterRequest
{
    [Required(ErrorMessage = "Indica el profesor.")]
    [StringLength(120)]
    public string Profesor { get; set; } = "";

    // Enlace del perfil (https://www.buhorater.com/perfil?id=...). Vacío quita el vínculo.
    [StringLength(300)]
    public string? Enlace { get; set; }
}
