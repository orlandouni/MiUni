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
public class ChatController(MiUniDbContext context, IRagClient ragClient, TimeProvider? clock = null,
    IConfiguration? chatConfiguration = null) : ControllerBase
{
    private const string UserRole = "user";
    private const string AssistantRole = "assistant";

    [HttpPost]
    public async Task<IActionResult> Send(ChatRequest request, CancellationToken ct)
    {
        var validation = await ValidateRequest(request, ct);
        if (validation.Result is not null) return validation.Result;

        await using var turn = await ChatTurnLease.TryAcquire(context, validation.UsuarioId, validation.SesionId, ct);
        if (turn is null) return Conflict(new { message = "Ya hay una respuesta en curso para esta conversaci\u00f3n." });
        if (request.SesionId.HasValue && !await SessionBelongsToUser(validation.SesionId, validation.UsuarioId, ct))
            return NotFound(new { message = "La sesion no existe." });
        var previous = await ContextRows(validation.SesionId, validation.UsuarioId, ct);
        var history = BoundedHistory(previous);
        var userMessage = CreateMessage(validation.SesionId, validation.UsuarioId, UserRole, validation.Mensaje);
        if (previous.Count > 0 && userMessage.FechaCreacion <= previous[^1].FechaCreacion)
            userMessage.FechaCreacion = previous[^1].FechaCreacion.AddTicks(10);
        context.Historialchats.Add(userMessage);
        await context.SaveChangesAsync(ct);

        string answer;
        try
        {
            var schedule = await PersonalSchedule.Resolve(context, validation.UsuarioId, validation.Mensaje,
                (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime, ct);
            answer = schedule.Answer ?? await BuildFastPlaceAnswer(validation.Mensaje, ct)
                ?? await ragClient.GetAnswerAsync(new RagChatInput(validation.Mensaje, history, schedule.Context),
                    GetAuthorizationHeader(), ct);
            if (string.IsNullOrWhiteSpace(answer)) throw new InvalidOperationException("Empty assistant answer");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "No se pudo obtener respuesta del asistente." });
        }

        var assistantMessage = CreateMessage(validation.SesionId, validation.UsuarioId, AssistantRole, answer);
        if (assistantMessage.FechaCreacion <= userMessage.FechaCreacion)
            assistantMessage.FechaCreacion = userMessage.FechaCreacion.AddTicks(10);
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

        await using var turn = await ChatTurnLease.TryAcquire(context, validation.UsuarioId, validation.SesionId, ct);
        if (turn is null) return Conflict(new { message = "Ya hay una respuesta en curso para esta conversaci\u00f3n." });
        if (request.SesionId.HasValue && !await SessionBelongsToUser(validation.SesionId, validation.UsuarioId, ct))
            return NotFound(new { message = "La sesion no existe." });
        var previous = await ContextRows(validation.SesionId, validation.UsuarioId, ct);
        var history = BoundedHistory(previous);
        var userMessage = CreateMessage(validation.SesionId, validation.UsuarioId, UserRole, validation.Mensaje);
        if (previous.Count > 0 && userMessage.FechaCreacion <= previous[^1].FechaCreacion)
            userMessage.FechaCreacion = previous[^1].FechaCreacion.AddTicks(10);
        context.Historialchats.Add(userMessage);
        await context.SaveChangesAsync(ct);

        Response.Headers["X-Session-Id"] = validation.SesionId.ToString();

        var answer = new StringBuilder();
        var wroteChunk = false;

        try
        {
            var schedule = await PersonalSchedule.Resolve(context, validation.UsuarioId, validation.Mensaje,
                (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime, ct);
            var fastAnswer = schedule.Answer ?? await BuildFastPlaceAnswer(validation.Mensaje, ct);
            if (fastAnswer is not null)
            {
                Response.ContentType = "text/plain; charset=utf-8";
                wroteChunk = true;
                answer.Append(fastAnswer);
                await Response.WriteAsync(fastAnswer, ct);
                await Response.Body.FlushAsync(ct);
            }
            else
            {
                var question = new RagChatInput(validation.Mensaje, history, schedule.Context);
                await foreach (var chunk in ragClient.StreamAnswerAsync(question, GetAuthorizationHeader(), ct))
                {
                    if (string.IsNullOrEmpty(chunk)) continue;

                    if (!wroteChunk) Response.ContentType = "text/plain; charset=utf-8";
                    wroteChunk = true;
                    answer.Append(chunk);
                    await Response.WriteAsync(chunk, ct);
                    await Response.Body.FlushAsync(ct);
                }
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

        if (!wroteChunk)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "El asistente devolvio una respuesta vacia." });
        }

        var fullAnswer = answer.ToString();
        if (!string.IsNullOrWhiteSpace(fullAnswer))
        {
            var saved = CreateMessage(validation.SesionId, validation.UsuarioId, AssistantRole, fullAnswer);
            if (saved.FechaCreacion <= userMessage.FechaCreacion)
                saved.FechaCreacion = userMessage.FechaCreacion.AddTicks(10);
            context.Historialchats.Add(saved);
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
            .OrderBy(h => h.FechaCreacion).ThenBy(h => h.Id)
            .ToListAsync(ct);

        var sessions = messages
            .GroupBy(h => h.SesionId)
            .Select(group =>
            {
                var ordered = group.OrderBy(h => h.FechaCreacion).ThenBy(h => h.Id).ToList();
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

    [HttpDelete("sessions/{sesionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sesionId, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        await using var turn = await ChatTurnLease.TryAcquire(context, usuarioId, sesionId, ct);
        if (turn is null) return Conflict(new { message = "A response is in progress for this conversation." });
        var messages = await context.Historialchats
            .Where(h => h.SesionId == sesionId && h.UsuarioId == usuarioId)
            .ToListAsync(ct);
        if (messages.Count == 0) return NotFound();
        context.Historialchats.RemoveRange(messages);
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> SessionBelongsToUser(Guid sesionId, Guid usuarioId, CancellationToken ct) =>
        await context.Historialchats.AsNoTracking()
            .AnyAsync(h => h.SesionId == sesionId && h.UsuarioId == usuarioId, ct);

    private async Task<List<ChatMessageDto>> GetMessages(Guid sesionId, Guid usuarioId, CancellationToken ct) =>
        await context.Historialchats.AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId && h.SesionId == sesionId)
            .OrderBy(h => h.FechaCreacion).ThenBy(h => h.Id)
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

    private string? GetAuthorizationHeader()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        return string.IsNullOrWhiteSpace(authorizationHeader) ? null : authorizationHeader;
    }

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

    private async Task<List<Historialchat>> ContextRows(Guid session, Guid user, CancellationToken ct)
    {
        var rows = await context.Historialchats.AsNoTracking()
            .Where(h => h.UsuarioId == user && h.SesionId == session)
            .OrderByDescending(h => h.FechaCreacion).ThenByDescending(h => h.Id)
            .Take(256).ToListAsync(ct);
        rows.Reverse();
        return rows;
    }

    private IReadOnlyList<RagHistoryMessage> BoundedHistory(List<Historialchat> rows) =>
        ChatContext.CompletedTurns(rows,
            chatConfiguration?.GetValue("Chat:HistoryMaxMessages", ChatContext.MaxMessages) ?? ChatContext.MaxMessages,
            chatConfiguration?.GetValue("Chat:HistoryMaxBytes", ChatContext.MaxHistoryBytes) ?? ChatContext.MaxHistoryBytes);

    private async Task<string?> BuildFastPlaceAnswer(string question, CancellationToken ct)
    {
        var places = await context.Lugars.AsNoTracking()
            .Where(place => place.Activo)
            .OrderBy(place => place.Nombre)
            .Select(place => new { place.Nombre, Categoria = place.Categoria.Nombre, place.Ubicacion })
            .ToListAsync(ct);

        // Filter in memory after the small catalog query: geography X/Y SQL casts
        // differ by provider, while map eligibility must stay identical in tests.
        var mapPlaces = places.Where(place => place.Ubicacion is { IsEmpty: false }
                && place.Ubicacion.X >= -180 && place.Ubicacion.X <= 180
                && place.Ubicacion.Y >= -90 && place.Ubicacion.Y <= 90)
            .Select(place => new ChatPlace(place.Nombre, place.Categoria));
        return ChatPlaceRouter.TryBuildAnswer(question, mapPlaces);
    }

    private record ValidatedChatRequest(
        IActionResult? Result,
        Guid UsuarioId = default,
        Guid SesionId = default,
        string Mensaje = "");
}
