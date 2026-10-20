using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;
using MiUni.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using MiUni.Api.Identity;
using NetTopologySuite.Geometries;
using Npgsql;

namespace MiUni.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LugarController : ControllerBase
{
    private readonly MiUniDbContext _context;

    public LugarController(MiUniDbContext context)
    {
        _context = context;
    }

    // GET /api/Lugar
// GET /api/Lugar?categoria=Comida
// GET /api/Lugar?q=5k
// GET /api/Lugar?q=biblio&categoria=Biblioteca
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] string? categoria, [FromQuery] string? q)
{
    var query = _context.Lugars
        .AsNoTracking()
        .Include(l => l.Categoria)
        .Where(l => l.Activo);

    if (!string.IsNullOrWhiteSpace(categoria))
    {
        query = query.Where(l => l.Categoria.Nombre == categoria);
    }

    if (!string.IsNullOrWhiteSpace(q))
    {
        var patron = $"%{q.Trim()}%";
        query = query.Where(l =>
            EF.Functions.ILike(l.Nombre, patron) ||
            (l.Descripcion != null && EF.Functions.ILike(l.Descripcion, patron)));
    }

    // Aquí SÍ se ejecuta el SQL (filtros de nombre/categoría se traducen bien)
    var lugaresEntidades = await query.OrderBy(l => l.Nombre).ToListAsync();

    // Mapeo a DTO en memoria (evita el problema de st_y con geography)
    var lugares = lugaresEntidades.Select(l => new LugarDto
    {
        Id = l.Id,
        Nombre = l.Nombre,
        Descripcion = l.Descripcion,
        Piso = l.Piso,
        Latitud = l.Ubicacion.Y,
        Longitud = l.Ubicacion.X,
        Categoria = new CategoriaDto
        {
            Id = l.Categoria.Id,
            Nombre = l.Categoria.Nombre,
            Icono = l.Categoria.Icono
        }
    }).ToList();

    return Ok(lugares);
}

    [HttpGet("admin"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(IEnumerable<LugarAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAdmin([FromQuery] bool? activo, CancellationToken ct)
    {
        var query = _context.Lugars.AsNoTracking().Include(l => l.Categoria).AsQueryable();
        if (activo.HasValue) query = query.Where(l => l.Activo == activo.Value);
        return Ok((await query.OrderBy(l => l.Nombre).ToListAsync(ct)).Select(AdminDto));
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(LugarAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerAdmin(Guid id, CancellationToken ct)
    {
        var lugar = await _context.Lugars.AsNoTracking().Include(l => l.Categoria)
            .SingleOrDefaultAsync(l => l.Id == id, ct);
        return lugar is null ? NotFound() : Ok(AdminDto(lugar));
    }

    [HttpPost, Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(LugarAdminDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarLugarRequest request, CancellationToken ct)
    {
        var categoria = await _context.Categoria.FindAsync([request.CategoriaId], ct);
        if (categoria is null) return BadRequest(new { message = "La categoría no existe." });
        var lugar = new Lugar { Id = Guid.NewGuid(), FechaCreacion = DateTime.UtcNow, Categoria = categoria };
        Aplicar(lugar, request);
        _context.Lugars.Add(lugar);
        var error = await Guardar(ct);
        return error ?? CreatedAtAction(nameof(ObtenerAdmin), new { id = lugar.Id }, AdminDto(lugar));
    }

    [HttpPut("{id:guid}"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Editar(Guid id, GuardarLugarRequest request, CancellationToken ct)
    {
        var lugar = await _context.Lugars.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (lugar is null) return NotFound();
        if (!await _context.Categoria.AnyAsync(c => c.Id == request.CategoriaId, ct))
            return BadRequest(new { message = "La categoría no existe." });
        if (request.Activo && lugar.PropietarioId.HasValue && lugar.EstadoSolicitud != "Aprobada")
            return Conflict(new { message = "Aprueba primero la solicitud del negocio para publicarlo." });
        Aplicar(lugar, request);
        return await Guardar(ct) ?? (IActionResult)NoContent();
    }

    // Baja lógica para preservar favoritos, reseñas y reportes.
    [HttpDelete("{id:guid}"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var lugar = await _context.Lugars.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (lugar is null) return NotFound();
        lugar.Activo = false;
        return await Guardar(ct) ?? (IActionResult)NoContent();
    }

    private static void Aplicar(Lugar lugar, GuardarLugarRequest request)
    {
        lugar.Nombre = request.Nombre.Trim();
        lugar.Descripcion = request.Descripcion?.Trim();
        lugar.CategoriaId = request.CategoriaId;
        lugar.Ubicacion = new Point(request.Longitud!.Value, request.Latitud!.Value) { SRID = 4326 };
        lugar.Piso = request.Piso;
        lugar.Activo = request.Activo;
    }

    private static LugarAdminDto AdminDto(Lugar l) => new(l.Id, l.Nombre, l.Descripcion, l.CategoriaId,
        l.Categoria.Nombre, l.Ubicacion.Y, l.Ubicacion.X, l.Piso, l.Activo,
        l.FechaCreacion, l.PropietarioId, l.EstadoSolicitud);

    private async Task<IActionResult?> Guardar(CancellationToken ct)
    {
        try { await _context.SaveChangesAsync(ct); return null; }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "El lugar cambió o ya no está disponible." }); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict(new { message = "La categoría ya no está disponible." }); }
    }
}
