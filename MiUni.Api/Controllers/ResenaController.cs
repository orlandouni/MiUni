using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Models;
using Npgsql;

namespace MiUni.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResenaController(MiUniDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? lugarId, CancellationToken ct)
    {
        var query = context.Resenas.AsNoTracking();
        if (lugarId.HasValue) query = query.Where(r => r.LugarId == lugarId.Value);
        return Ok(await query.OrderByDescending(r => r.FechaCreacion)
            .Select(r => new ResenaDto(r.Id, r.UsuarioId, r.LugarId, r.Calificacion,
                r.Comentario, r.FechaCreacion)).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var resena = await context.Resenas.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        return resena is null ? NotFound() : Ok(ToDto(resena));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CrearResenaRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (!await context.Lugars.AnyAsync(l => l.Id == request.LugarId && l.Activo, ct))
            return BadRequest(new { message = "El lugar no existe o no está activo." });

        var resena = new Resena
        {
            Id = Guid.NewGuid(), UsuarioId = usuarioId, LugarId = request.LugarId,
            Calificacion = request.Calificacion, Comentario = request.Comentario?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };
        context.Resenas.Add(resena);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Conflict(new { message = "El usuario o el lugar ya no está disponible." });
        }
        return CreatedAtAction(nameof(GetById), new { id = resena.Id }, ToDto(resena));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ActualizarResenaRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var resena = await context.Resenas.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (resena is null) return NotFound();
        if (resena.UsuarioId != usuarioId) return Forbid();
        resena.Calificacion = request.Calificacion;
        resena.Comentario = request.Comentario?.Trim();
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var resena = await context.Resenas.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (resena is null) return NotFound();
        if (resena.UsuarioId != usuarioId) return Forbid();
        // Conserva las reseñas vinculadas a reportes para no perder información de moderación.
        if (await context.Reporteusuarios.AnyAsync(r => r.ResenaId == id, ct))
            return Conflict(new { message = "La reseña tiene reportes asociados y no se puede eliminar." });
        context.Resenas.Remove(resena);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Conflict(new { message = "La reseña tiene registros asociados y no se puede eliminar." });
        }
        return NoContent();
    }

    private static ResenaDto ToDto(Resena r) =>
        new(r.Id, r.UsuarioId, r.LugarId, r.Calificacion, r.Comentario, r.FechaCreacion);
}
