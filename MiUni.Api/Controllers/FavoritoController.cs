using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Models;
using Npgsql;

namespace MiUni.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FavoritoController(MiUniDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        return Ok(await context.Favoritos.AsNoTracking().Where(f => f.UsuarioId == usuarioId)
            .OrderByDescending(f => f.FechaAgregado)
            .Select(f => new FavoritoDto(f.LugarId, f.FechaAgregado)).ToListAsync(ct));
    }

    [HttpGet("{lugarId:guid}")]
    public async Task<IActionResult> GetById(Guid lugarId, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var favorito = await context.Favoritos.AsNoTracking()
            .SingleOrDefaultAsync(f => f.UsuarioId == usuarioId && f.LugarId == lugarId, ct);
        return favorito is null ? NotFound() : Ok(new FavoritoDto(favorito.LugarId, favorito.FechaAgregado));
    }

    [HttpPost]
    public Task<IActionResult> Create(CrearFavoritoRequest request, CancellationToken ct) =>
        Save(request.LugarId, false, ct);

    // La clave es (usuario, lugar); no hay campos editables. PUT asegura su existencia.
    [HttpPut("{lugarId:guid}")]
    public Task<IActionResult> Put(Guid lugarId, CancellationToken ct) => Save(lugarId, true, ct);

    private async Task<IActionResult> Save(Guid lugarId, bool idempotent, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (await context.Favoritos.AnyAsync(f => f.UsuarioId == usuarioId && f.LugarId == lugarId, ct))
            return idempotent ? NoContent() : Conflict(new { message = "El lugar ya está en tus favoritos." });
        if (!await context.Lugars.AnyAsync(l => l.Id == lugarId && l.Activo, ct))
            return BadRequest(new { message = "El lugar no existe o no está activo." });
        var favorito = new Favorito { UsuarioId = usuarioId, LugarId = lugarId, FechaAgregado = DateTime.UtcNow };
        context.Favoritos.Add(favorito);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return idempotent ? NoContent() : Conflict(new { message = "El lugar ya está en tus favoritos." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Conflict(new { message = "El usuario o el lugar ya no está disponible." });
        }
        return CreatedAtAction(nameof(GetById), new { lugarId }, new FavoritoDto(lugarId, favorito.FechaAgregado));
    }

    [HttpDelete("{lugarId:guid}")]
    public async Task<IActionResult> Delete(Guid lugarId, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var favorito = await context.Favoritos.SingleOrDefaultAsync(
            f => f.UsuarioId == usuarioId && f.LugarId == lugarId, ct);
        if (favorito is null) return NotFound();
        context.Favoritos.Remove(favorito);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    private bool TryGetUsuario(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
