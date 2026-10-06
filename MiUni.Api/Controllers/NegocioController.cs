using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using NetTopologySuite.Geometries;

namespace MiUni.Api.Controllers;

[ApiController, Authorize, Route("api/Negocio")]
public class NegocioController(MiUniDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static NegocioDto Dto(Lugar lugar) => new(lugar.Id, lugar.Nombre, lugar.Descripcion,
        lugar.PropietarioId, lugar.EstadoSolicitud, lugar.Ubicacion.Y, lugar.Ubicacion.X);

    [HttpGet("mios")]
    public async Task<IActionResult> Mios(CancellationToken ct)
    {
        var negocios = await db.Lugars.AsNoTracking().Where(l => l.PropietarioId == UsuarioId)
            .OrderBy(l => l.Nombre).ToListAsync(ct);
        return Ok(negocios.Select(Dto));
    }

    [HttpPost("solicitudes")]
    public async Task<IActionResult> Solicitar(SolicitarNegocioRequest request, CancellationToken ct)
    {
        if (!await db.Categoria.AnyAsync(c => c.Id == request.CategoriaId, ct))
            return BadRequest(new { message = "Selecciona una categoría válida." });
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(), Nombre = request.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(),
            CategoriaId = request.CategoriaId, PropietarioId = UsuarioId,
            Ubicacion = new Point(request.Longitud!.Value, request.Latitud!.Value) { SRID = 4326 },
            Activo = false, EstadoSolicitud = "Pendiente", FechaCreacion = DateTime.UtcNow
        };
        db.Lugars.Add(lugar);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, Dto(lugar));
    }

    [HttpGet("solicitudes"), Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Pendientes(CancellationToken ct)
    {
        var pendientes = await db.Lugars.AsNoTracking().Where(l => l.EstadoSolicitud == "Pendiente")
            .OrderBy(l => l.FechaCreacion).ToListAsync(ct);
        return Ok(pendientes.Select(Dto));
    }

    [HttpPost("solicitudes/{id:guid}/aprobar"), Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Aprobar(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lugar = await db.Lugars.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId != null, ct);
        if (lugar is null) return NotFound();
        if (lugar.EstadoSolicitud != "Pendiente") return Conflict(new { message = "La solicitud ya fue revisada." });
        var owner = await users.FindByIdAsync(lugar.PropietarioId!.Value.ToString());
        if (owner is null) return NotFound();
        if (!await users.IsInRoleAsync(owner, AppRoles.Propietario))
        {
            var result = await users.AddToRoleAsync(owner, AppRoles.Propietario);
            if (!result.Succeeded) return Conflict(new { message = "No fue posible asignar el rol. Intenta nuevamente." });
        }
        lugar.EstadoSolicitud = "Aprobada";
        lugar.Activo = true;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "La solicitud ya fue revisada." }); }
        await transaction.CommitAsync(ct);
        return Ok(Dto(lugar));
    }

    [HttpPost("solicitudes/{id:guid}/rechazar"), Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Rechazar(Guid id, CancellationToken ct)
    {
        var lugar = await db.Lugars.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId != null, ct);
        if (lugar is null) return NotFound();
        if (lugar.EstadoSolicitud != "Pendiente") return Conflict(new { message = "La solicitud ya fue revisada." });
        lugar.EstadoSolicitud = "Rechazada";
        lugar.Activo = false;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "La solicitud ya fue revisada." }); }
        return NoContent();
    }

    [HttpPut("{id:guid}"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> Editar(Guid id, EditarNegocioRequest request, CancellationToken ct)
    {
        var lugar = await db.Lugars.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId != null, ct);
        if (lugar is null) return NotFound();
        if (lugar.PropietarioId != UsuarioId && !User.IsInRole(AppRoles.Administrador)) return Forbid();
        if (lugar.EstadoSolicitud != "Aprobada") return Conflict(new { message = "El negocio aún no está aprobado." });
        lugar.Nombre = request.Nombre.Trim();
        lugar.Descripcion = request.Descripcion?.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(Dto(lugar));
    }
}
