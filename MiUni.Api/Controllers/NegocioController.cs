using System.Globalization;
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

    // Las coordenadas se leen en memoria: proyectar ST_Y/ST_X sobre geography no se traduce.
    private async Task<List<NegocioDto>> Proyectar(IQueryable<Lugar> query, CancellationToken ct)
    {
        var filas = await query.AsNoTracking().Select(l => new
        {
            Lugar = l, Categoria = l.Categoria.Nombre, Resenas = l.Resenas.Count,
            Promedio = l.Resenas.Average(r => (double?)r.Calificacion), Favoritos = l.Favoritos.Count
        }).ToListAsync(ct);
        return filas.Select(f => new NegocioDto(f.Lugar.Id, f.Lugar.Nombre, f.Lugar.Descripcion, f.Lugar.PropietarioId,
            f.Lugar.EstadoSolicitud, f.Lugar.Ubicacion.Y, f.Lugar.Ubicacion.X, f.Lugar.CategoriaId, f.Categoria,
            f.Lugar.EsAmbulante, f.Lugar.Activo, f.Lugar.FechaCreacion, f.Resenas, f.Promedio, f.Favoritos)).ToList();
    }

    private async Task<NegocioDto> Dto(Guid id, CancellationToken ct) =>
        (await Proyectar(db.Lugars.Where(l => l.Id == id), ct)).Single();

    // Devuelve el negocio sólo si pertenece al usuario (o si es administrador).
    private async Task<(Lugar? lugar, IActionResult? error)> Propio(Guid id, CancellationToken ct)
    {
        var lugar = await db.Lugars.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId != null, ct);
        if (lugar is null) return (null, NotFound());
        if (lugar.PropietarioId != UsuarioId && !User.IsInRole(AppRoles.Administrador)) return (null, Forbid());
        return (lugar, null);
    }

    [HttpGet("mios")]
    public async Task<IActionResult> Mios(CancellationToken ct) =>
        Ok(await Proyectar(db.Lugars.Where(l => l.PropietarioId == UsuarioId).OrderBy(l => l.Nombre), ct));

    [HttpPost("solicitudes")]
    public async Task<IActionResult> Solicitar(SolicitarNegocioRequest request, CancellationToken ct)
    {
        if (!await db.Categoria.AnyAsync(c => c.Id == request.CategoriaId, ct))
            return BadRequest(new { message = "Selecciona una categoría válida." });
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(), Nombre = request.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(),
            CategoriaId = request.CategoriaId, PropietarioId = UsuarioId, EsAmbulante = request.EsAmbulante,
            Ubicacion = new Point(request.Longitud!.Value, request.Latitud!.Value) { SRID = 4326 },
            Activo = false, EstadoSolicitud = "Pendiente", FechaCreacion = DateTime.UtcNow
        };
        db.Lugars.Add(lugar);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, await Dto(lugar.Id, ct));
    }

    [HttpGet("solicitudes"), Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Pendientes(CancellationToken ct) =>
        Ok(await Proyectar(db.Lugars.Where(l => l.EstadoSolicitud == "Pendiente").OrderBy(l => l.FechaCreacion), ct));

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
        return Ok(await Dto(id, ct));
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

    // Un negocio pendiente también puede corregirse antes de la revisión; uno rechazado no.
    [HttpPut("{id:guid}"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> Editar(Guid id, EditarNegocioRequest request, CancellationToken ct)
    {
        var (lugar, error) = await Propio(id, ct);
        if (error is not null) return error;
        if (lugar!.EstadoSolicitud == "Rechazada") return Conflict(new { message = "El negocio fue rechazado y no se puede editar." });
        if (request.CategoriaId.HasValue && !await db.Categoria.AnyAsync(c => c.Id == request.CategoriaId, ct))
            return BadRequest(new { message = "Selecciona una categoría válida." });
        lugar.Nombre = request.Nombre.Trim();
        lugar.Descripcion = request.Descripcion?.Trim();
        if (request.CategoriaId.HasValue) lugar.CategoriaId = request.CategoriaId.Value;
        if (request.Latitud.HasValue)
            lugar.Ubicacion = new Point(request.Longitud!.Value, request.Latitud.Value) { SRID = 4326 };
        if (request.EsAmbulante.HasValue) lugar.EsAmbulante = request.EsAmbulante.Value;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "El negocio cambió. Recarga e intenta de nuevo." }); }
        return Ok(await Dto(id, ct));
    }

    // Todo lo que el propietario administra de un negocio en una sola respuesta.
    [HttpGet("{id:guid}/panel")]
    public async Task<IActionResult> Panel(Guid id, CancellationToken ct)
    {
        var (_, error) = await Propio(id, ct);
        if (error is not null) return error;
        var resenas = await db.Resenas.AsNoTracking().Where(r => r.LugarId == id)
            .OrderByDescending(r => r.FechaCreacion).Take(30)
            .Select(r => new ResenaNegocioDto(r.Id, r.Usuario.Nombre, r.Calificacion, r.Comentario, r.FechaCreacion))
            .ToListAsync(ct);
        return Ok(new PanelNegocioDto(await Dto(id, ct), await Horarios(id, ct), await Productos(id, false, ct), resenas));
    }

    // Información pública de un negocio publicado para su ficha.
    [HttpGet("{id:guid}/info"), AllowAnonymous]
    public async Task<IActionResult> Info(Guid id, CancellationToken ct)
    {
        var lugar = await db.Lugars.AsNoTracking().Where(l => l.Id == id && l.Activo)
            .Select(l => new { l.EsAmbulante }).SingleOrDefaultAsync(ct);
        if (lugar is null) return NotFound();
        return Ok(new InfoNegocioDto(lugar.EsAmbulante, await Horarios(id, ct), await Productos(id, true, ct)));
    }

    [HttpPut("{id:guid}/horarios"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> GuardarHorarios(Guid id, GuardarHorariosRequest request, CancellationToken ct)
    {
        var (_, error) = await Propio(id, ct);
        if (error is not null) return error;
        var nuevos = new List<Horariooperacion>();
        foreach (var h in request.Horarios)
        {
            var abre = TimeOnly.ParseExact(h.HoraApertura, "HH:mm", CultureInfo.InvariantCulture);
            var cierra = TimeOnly.ParseExact(h.HoraCierre, "HH:mm", CultureInfo.InvariantCulture);
            if (cierra <= abre) return BadRequest(new { message = "La hora de cierre debe ser posterior a la de apertura." });
            nuevos.Add(new Horariooperacion { Id = Guid.NewGuid(), LugarId = id, DiaSemana = h.DiaSemana, HoraApertura = abre, HoraCierre = cierra });
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Horariooperacions.RemoveRange(await db.Horariooperacions.Where(h => h.LugarId == id).ToListAsync(ct));
        db.Horariooperacions.AddRange(nuevos);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(await Horarios(id, ct));
    }

    [HttpPost("{id:guid}/productos"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> CrearProducto(Guid id, GuardarProductoRequest request, CancellationToken ct)
    {
        var (_, error) = await Propio(id, ct);
        if (error is not null) return error;
        var producto = new Productomenu { Id = Guid.NewGuid(), LugarId = id };
        AplicarProducto(producto, request);
        db.Productomenus.Add(producto);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, ProductoDto(producto));
    }

    [HttpPut("{id:guid}/productos/{productoId:guid}"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> EditarProducto(Guid id, Guid productoId, GuardarProductoRequest request, CancellationToken ct)
    {
        var (_, error) = await Propio(id, ct);
        if (error is not null) return error;
        var producto = await db.Productomenus.SingleOrDefaultAsync(p => p.Id == productoId && p.LugarId == id, ct);
        if (producto is null) return NotFound();
        AplicarProducto(producto, request);
        await db.SaveChangesAsync(ct);
        return Ok(ProductoDto(producto));
    }

    [HttpDelete("{id:guid}/productos/{productoId:guid}"), Authorize(Roles = AppRoles.GestionNegocios)]
    public async Task<IActionResult> EliminarProducto(Guid id, Guid productoId, CancellationToken ct)
    {
        var (_, error) = await Propio(id, ct);
        if (error is not null) return error;
        var producto = await db.Productomenus.SingleOrDefaultAsync(p => p.Id == productoId && p.LugarId == id, ct);
        if (producto is null) return NotFound();
        db.Productomenus.Remove(producto);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<List<HorarioDto>> Horarios(Guid id, CancellationToken ct) =>
        (await db.Horariooperacions.AsNoTracking().Where(h => h.LugarId == id)
            .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraApertura).ToListAsync(ct))
        .Select(h => new HorarioDto(h.DiaSemana, h.HoraApertura.ToString("HH:mm"), h.HoraCierre.ToString("HH:mm"))).ToList();

    private async Task<List<ProductoDto>> Productos(Guid id, bool soloDisponibles, CancellationToken ct) =>
        await db.Productomenus.AsNoTracking().Where(p => p.LugarId == id && (!soloDisponibles || p.EstaDisponible))
            .OrderBy(p => p.Nombre).Select(p => new ProductoDto(p.Id, p.Nombre, p.Descripcion, p.Precio, p.EstaDisponible))
            .ToListAsync(ct);

    private static void AplicarProducto(Productomenu producto, GuardarProductoRequest request)
    {
        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        producto.Precio = decimal.Round(request.Precio, 2);
        producto.EstaDisponible = request.EstaDisponible;
    }

    private static ProductoDto ProductoDto(Productomenu p) => new(p.Id, p.Nombre, p.Descripcion, p.Precio, p.EstaDisponible);
}
