using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public record ResenaDto(Guid Id, Guid UsuarioId, Guid LugarId, short Calificacion,
    string? Comentario, DateTime FechaCreacion);

public class CrearResenaRequest : ActualizarResenaRequest
{
    public Guid LugarId { get; set; }
}

public class ActualizarResenaRequest
{
    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public short Calificacion { get; set; }

    public string? Comentario { get; set; }
}
