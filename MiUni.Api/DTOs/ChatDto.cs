using System.ComponentModel.DataAnnotations;

namespace MiUni.Api.DTOs;

public record ChatMessageDto(
    Guid Id,
    Guid SesionId,
    string Rol,
    string Contenido,
    DateTime FechaCreacion);

public record ChatSessionDto(
    Guid SesionId,
    DateTime FechaCreacion,
    DateTime UltimaActividad,
    string? Preview);

public class ChatRequest
{
    public Guid? SesionId { get; set; }

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    public string Mensaje { get; set; } = null!;
}

public record ChatResponse(Guid SesionId, string Answer, IReadOnlyList<ChatMessageDto> Messages);
