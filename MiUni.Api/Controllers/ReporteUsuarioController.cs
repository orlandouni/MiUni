using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using Npgsql;

namespace MiUni.Api.Controllers;

[ApiController, Authorize, Route("api/ReporteUsuario")]
public class ReporteUsuarioController(MiUniDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool EsAdmin => User.IsInRole(AppRoles.Administrador);
    private IQueryable<Reporteusuario> Visibles => db.Reporteusuarios
        .Where(r => EsAdmin || r.UsuarioId == UsuarioId);

    private static IQueryable<ReporteUsuarioDto> Proyectar(IQueryable<Reporteusuario> query) => query.Select(r =>
        new ReporteUsuarioDto(r.Id, r.UsuarioId, r.Usuario.Nombre, r.Usuario.Email, r.LugarId, r.ResenaId,
            r.LugarId ?? (r.Resena != null ? r.Resena.LugarId : null),
            r.Lugar != null ? r.Lugar.Nombre : r.Resena != null ? r.Resena.Lugar.Nombre : null,
            r.Resena != null ? r.Resena.Comentario : null, r.Motivo, r.Estado, r.FechaCreacion));

    // El usuario sólo ve sus reportes. El administrador ve todos.
    [HttpGet]
    [ProducesResponseType(typeof(PaginaReportesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] EstadoReporte? estado,
        [FromQuery] Guid? lugarId, [FromQuery] Guid? resenaId,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamanoPagina = 20, CancellationToken ct = default)
    {
        if (estado.HasValue && !Enum.IsDefined(estado.Value)) return BadRequest(new { message = "Estado inválido." });
        var query = Visibles.AsNoTracking();
        if (estado.HasValue) query = query.Where(r => r.Estado == estado.Value);
        if (lugarId.HasValue) query = query.Where(r => r.LugarId == lugarId || (r.Resena != null && r.Resena.LugarId == lugarId));
        if (resenaId.HasValue) query = query.Where(r => r.ResenaId == resenaId);
        var total = await query.CountAsync(ct);
        var salto = ((long)pagina - 1) * tamanoPagina;
        var items = salto > int.MaxValue ? [] : await Proyectar(query.OrderByDescending(r => r.FechaCreacion)
            .ThenBy(r => r.Id).Skip((int)salto).Take(tamanoPagina)).ToListAsync(ct);
        return Ok(new PaginaReportesDto(items, total, pagina, tamanoPagina));
    }

    [HttpGet("resumen"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(IEnumerable<ResumenReporteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resumen(CancellationToken ct)
    {
        var grupos = await db.Reporteusuarios.GroupBy(r => r.Estado)
            .Select(g => new { estado = g.Key, cantidad = g.Count() }).ToListAsync(ct);
        return Ok(Enum.GetValues<EstadoReporte>().Select(e =>
            new ResumenReporteDto(e, grupos.FirstOrDefault(g => g.estado == e)?.cantidad ?? 0)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReporteUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var dto = await Proyectar(Visibles.AsNoTracking().Where(r => r.Id == id)).SingleOrDefaultAsync(ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReporteUsuarioDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(CrearReporteRequest request, CancellationToken ct)
    {
        // También protege las invocaciones directas desde otros servicios/pruebas.
        if (request.LugarId.HasValue == request.ResenaId.HasValue || request.LugarId == Guid.Empty || request.ResenaId == Guid.Empty)
            return BadRequest(new { message = "Indica exactamente un LugarId o ResenaId válido." });
        if (request.LugarId.HasValue && !await db.Lugars.AnyAsync(l => l.Id == request.LugarId && l.Activo, ct))
            return BadRequest(new { message = "El lugar no existe o no está activo." });
        if (request.ResenaId.HasValue && !await db.Resenas.AnyAsync(r => r.Id == request.ResenaId && r.Lugar.Activo, ct))
            return BadRequest(new { message = "La reseña no existe o su lugar no está activo." });
        if (await db.Reporteusuarios.AnyAsync(r => r.UsuarioId == UsuarioId &&
                (request.LugarId.HasValue ? r.LugarId == request.LugarId : r.ResenaId == request.ResenaId), ct))
            return Conflict(new { message = "Ya reportaste este lugar o reseña." });
        var reporte = new Reporteusuario
        {
            Id = Guid.NewGuid(), UsuarioId = UsuarioId, LugarId = request.LugarId, ResenaId = request.ResenaId,
            Motivo = request.Motivo.Trim(), Estado = EstadoReporte.Pendiente, FechaCreacion = DateTime.UtcNow
        };
        db.Reporteusuarios.Add(reporte);
        var error = await Guardar(ct);
        if (error is not null) return error;
        var dto = await Proyectar(db.Reporteusuarios.Where(r => r.Id == reporte.Id)).SingleAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = reporte.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Editar(Guid id, EditarReporteRequest request, CancellationToken ct)
    {
        var reporte = await Visibles.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (reporte is null) return NotFound();
        if (reporte.Estado != EstadoReporte.Pendiente)
            return Conflict(new { message = "Sólo se puede editar el motivo de un reporte pendiente." });
        reporte.Motivo = request.Motivo.Trim();
        return await Guardar(ct) ?? (IActionResult)NoContent();
    }

    [HttpPatch("{id:guid}/estado"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CambiarEstado(Guid id, EstadoReporteRequest request, CancellationToken ct)
    {
        var reporte = await db.Reporteusuarios.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (reporte is null) return NotFound();
        reporte.Estado = request.Estado!.Value;
        return await Guardar(ct) ?? (IActionResult)NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var reporte = await Visibles.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (reporte is null) return NotFound();
        if (!EsAdmin && reporte.Estado != EstadoReporte.Pendiente)
            return Conflict(new { message = "Sólo puedes retirar reportes pendientes." });
        db.Reporteusuarios.Remove(reporte);
        return await Guardar(ct) ?? (IActionResult)NoContent();
    }

    private async Task<IActionResult?> Guardar(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return null; }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "El reporte cambió. Actualiza la lista e inténtalo de nuevo." }); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict(new { message = "Ya reportaste este lugar o reseña." }); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict(new { message = "El usuario, lugar o reseña ya no está disponible." }); }
    }
}
