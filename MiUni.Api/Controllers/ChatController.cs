using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Models;
using MiUni.Api.Services;
using System.Text;

namespace MiUni.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController(MiUniDbContext context, IRagClient ragClient) : ControllerBase
{
    private const string UserRole = "user";
    private const string AssistantRole = "assistant";

    [HttpPost]
    public async Task<IActionResult> Send(ChatRequest request, CancellationToken ct)
    {
        var validation = await ValidateRequest(request, ct);
        if (validation.Result is not null) return validation.Result;

        var userMessage = CreateMessage(validation.SesionId, validation.UsuarioId, UserRole, validation.Mensaje);
        context.Historialchats.Add(userMessage);
        await context.SaveChangesAsync(ct);

        string answer;
        try
        {
            answer = await ragClient.GetAnswerAsync(validation.Mensaje, ct);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "No se pudo obtener respuesta del asistente." });
        }

        var assistantMessage = CreateMessage(validation.SesionId, validation.UsuarioId, AssistantRole, answer);
        context.Historialchats.Add(assistantMessage);
        await context.SaveChangesAsync(ct);

        var messages = await GetMessages(validation.SesionId, validation.UsuarioId, ct);
        return Ok(new ChatResponse(validation.SesionId, answer, messages));
    }

    [HttpPost("stream")]
    public async Task<IActionResult> Stream(ChatRequest request, CancellationToken ct)
    {
        var validation = await ValidateRequest(request, ct);
        if (validation.Result is not null) return validation.Result;

        var userMessage = CreateMessage(validation.SesionId, validation.UsuarioId, UserRole, validation.Mensaje);
        context.Historialchats.Add(userMessage);
        await context.SaveChangesAsync(ct);

        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers["X-Session-Id"] = validation.SesionId.ToString();

        var answer = new StringBuilder();
        var wroteChunk = false;

        try
        {
            await foreach (var chunk in ragClient.StreamAnswerAsync(validation.Mensaje, ct))
            {
                if (string.IsNullOrEmpty(chunk)) continue;

                wroteChunk = true;
                answer.Append(chunk);
                await Response.WriteAsync(chunk, ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (Exception)
        {
            if (!wroteChunk && !Response.HasStarted)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "No se pudo obtener respuesta del asistente." });
            }

            return new EmptyResult();
        }

        var fullAnswer = answer.ToString();
        if (!string.IsNullOrWhiteSpace(fullAnswer))
        {
            context.Historialchats.Add(CreateMessage(
                validation.SesionId,
                validation.UsuarioId,
                AssistantRole,
                fullAnswer));
            await context.SaveChangesAsync(ct);
        }

        return new EmptyResult();
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();

        var messages = await context.Historialchats.AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId)
            .OrderBy(h => h.FechaCreacion)
            .ToListAsync(ct);

        var sessions = messages
            .GroupBy(h => h.SesionId)
            .Select(group =>
            {
                var ordered = group.OrderBy(h => h.FechaCreacion).ToList();
                var preview = ordered.FirstOrDefault(h => h.Rol == UserRole)?.Contenido
                    ?? ordered.FirstOrDefault()?.Contenido;

                return new ChatSessionDto(
                    group.Key,
                    ordered.First().FechaCreacion,
                    ordered.Last().FechaCreacion,
                    preview);
            })
            .OrderByDescending(s => s.UltimaActividad)
            .ToList();

        return Ok(sessions);
    }

    [HttpGet("sessions/{sesionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sesionId, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();

        var messages = await GetMessages(sesionId, usuarioId, ct);
        return messages.Count == 0 ? NotFound() : Ok(messages);
    }

    private async Task<bool> SessionBelongsToUser(Guid sesionId, Guid usuarioId, CancellationToken ct) =>
        await context.Historialchats.AsNoTracking()
            .AnyAsync(h => h.SesionId == sesionId && h.UsuarioId == usuarioId, ct);

    private async Task<List<ChatMessageDto>> GetMessages(Guid sesionId, Guid usuarioId, CancellationToken ct) =>
        await context.Historialchats.AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId && h.SesionId == sesionId)
            .OrderBy(h => h.FechaCreacion)
            .Select(h => new ChatMessageDto(h.Id, h.SesionId, h.Rol, h.Contenido, h.FechaCreacion))
            .ToListAsync(ct);

    private static Historialchat CreateMessage(Guid sesionId, Guid usuarioId, string rol, string contenido) =>
        new()
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            UsuarioId = usuarioId,
            Rol = rol,
            Contenido = contenido,
            FechaCreacion = DateTime.UtcNow
        };

    private bool TryGetUsuario(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

    private async Task<ValidatedChatRequest> ValidateRequest(ChatRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return new(Unauthorized());
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return new(Unauthorized());

        var mensaje = request.Mensaje.Trim();
        if (string.IsNullOrWhiteSpace(mensaje))
            return new(BadRequest(new { message = "El mensaje no puede estar vacio." }));

        var sesionId = request.SesionId ?? Guid.NewGuid();
        if (request.SesionId.HasValue && !await SessionBelongsToUser(sesionId, usuarioId, ct))
            return new(NotFound(new { message = "La sesion no existe." }));

        return new(null, usuarioId, sesionId, mensaje);
    }

    private record ValidatedChatRequest(
        IActionResult? Result,
        Guid UsuarioId = default,
        Guid SesionId = default,
        string Mensaje = "");
}
