using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using Npgsql;

namespace MiUni.Api.Controllers;

// Endpoints exclusivos del panel: métricas, usuarios, moderación de reseñas,
// historial de negocios y catálogo de carreras. Lugares, categorías, reportes y
// aprobación de negocios siguen en sus controladores originales.
[ApiController, Authorize(Roles = AppRoles.Administrador), Route("api/Admin")]
public class AdminController(MiUniDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    private static readonly string[] RolesAsignables = [AppRoles.Propietario, AppRoles.Administrador];
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("panel")]
    [ProducesResponseType(typeof(PanelAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Panel(CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var hace30 = ahora.AddDays(-30);
        var hace7 = ahora.AddDays(-7);
        var inicioSerie = ahora.Date.AddDays(-13);

        var registros = await db.Users.Where(u => u.FechaRegistro >= inicioSerie)
            .Select(u => u.FechaRegistro).ToListAsync(ct);
        var serie = Enumerable.Range(0, 14).Select(i => inicioSerie.AddDays(i)).Select(dia =>
            new ConteoDto(dia.ToString("yyyy-MM-dd"), registros.Count(r => r.Date == dia))).ToList();

        var porCategoria = await db.Categoria.Select(c => new
        {
            c.Nombre, Cantidad = c.Lugars.Count(l => l.Activo)
        }).OrderByDescending(c => c.Cantidad).ToListAsync(ct);

        var destacados = await db.Lugars.Where(l => l.Activo)
            .OrderByDescending(l => l.Resenas.Count).ThenByDescending(l => l.Favoritos.Count).Take(5)
            .Select(l => new LugarDestacadoDto(l.Id, l.Nombre, l.Resenas.Count,
                l.Resenas.Average(r => (double?)r.Calificacion), l.Favoritos.Count))
            .ToListAsync(ct);

        var mensajes = db.Historialchats.Where(h => h.FechaCreacion >= hace7);
        return Ok(new PanelAdminDto(
            await db.Users.CountAsync(ct),
            await db.Users.CountAsync(u => u.FechaRegistro >= hace30, ct),
            await db.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow, ct),
            await db.Lugars.CountAsync(l => l.Activo, ct),
            await db.Lugars.CountAsync(l => !l.Activo, ct),
            await db.Categoria.CountAsync(ct),
            await db.Resenas.CountAsync(ct),
            await db.Resenas.AverageAsync(r => (double?)r.Calificacion, ct),
            await db.Reporteusuarios.CountAsync(r => r.Estado == EstadoReporte.Pendiente, ct),
            await db.Lugars.CountAsync(l => l.EstadoSolicitud == "Pendiente", ct),
            await mensajes.Select(h => h.SesionId).Distinct().CountAsync(ct),
            await mensajes.CountAsync(h => h.Rol == "user", ct),
            serie,
            porCategoria.Select(c => new ConteoDto(c.Nombre, c.Cantidad)),
            destacados));
    }

    // ---------- Usuarios ----------

    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(PaginaDto<UsuarioAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Usuarios([FromQuery] string? q, [FromQuery] string? rol,
        [FromQuery] bool? bloqueado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Nombre, patron) ||
                                     (u.Email != null && EF.Functions.ILike(u.Email, patron)));
        }
        if (!string.IsNullOrWhiteSpace(rol))
        {
            var rolId = await db.Roles.Where(r => r.Name == rol).Select(r => (Guid?)r.Id).SingleOrDefaultAsync(ct);
            if (rolId is null) return BadRequest(new { message = "Rol inválido." });
            query = query.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == rolId));
        }
        if (bloqueado.HasValue)
        {
            var ahora = DateTimeOffset.UtcNow;
            query = bloqueado.Value
                ? query.Where(u => u.LockoutEnd != null && u.LockoutEnd > ahora)
                : query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= ahora);
        }

        var total = await query.CountAsync(ct);
        var items = await ProyectarUsuarios(query.OrderByDescending(u => u.FechaRegistro).ThenBy(u => u.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina), ct);
        return Ok(new PaginaDto<UsuarioAdminDto>(items, total, pagina, tamanoPagina));
    }

    [HttpGet("usuarios/{id:guid}")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Usuario(Guid id, CancellationToken ct)
    {
        var items = await ProyectarUsuarios(db.Users.AsNoTracking().Where(u => u.Id == id), ct);
        return items.Count == 0 ? NotFound() : Ok(items[0]);
    }

    // El rol Estudiante es la base de toda cuenta; sólo se administran los roles adicionales.
    [HttpPut("usuarios/{id:guid}/roles")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CambiarRoles(Guid id, RolesUsuarioRequest request, CancellationToken ct)
    {
        var solicitados = request.Roles.Select(r => r.Trim()).Where(r => r != AppRoles.Usuario).Distinct().ToList();
        if (solicitados.Any(r => !RolesAsignables.Contains(r)))
            return BadRequest(new { message = "Sólo se pueden asignar los roles Propietario y Administrador." });
        var usuario = await users.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();
        if (id == UsuarioId && !solicitados.Contains(AppRoles.Administrador))
            return Conflict(new { message = "No puedes quitarte tu propio rol de administrador." });

        var actuales = await users.GetRolesAsync(usuario);
        var agregar = solicitados.Except(actuales).ToList();
        var quitar = actuales.Where(r => RolesAsignables.Contains(r) && !solicitados.Contains(r)).ToList();
        if (!actuales.Contains(AppRoles.Usuario)) agregar.Add(AppRoles.Usuario);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (agregar.Count > 0 && !(await users.AddToRolesAsync(usuario, agregar)).Succeeded)
            return Conflict(new { message = "No fue posible asignar los roles." });
        if (quitar.Count > 0 && !(await users.RemoveFromRolesAsync(usuario, quitar)).Succeeded)
            return Conflict(new { message = "No fue posible retirar los roles." });
        await transaction.CommitAsync(ct);
        return await Usuario(id, ct);
    }

    // El bloqueo usa el lockout de Identity: impide iniciar sesión e invalida los JWT vigentes.
    [HttpPost("usuarios/{id:guid}/bloquear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Bloquear(Guid id, CancellationToken ct) => CambiarBloqueo(id, true, ct);

    [HttpPost("usuarios/{id:guid}/desbloquear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Desbloquear(Guid id, CancellationToken ct) => CambiarBloqueo(id, false, ct);

    private async Task<IActionResult> CambiarBloqueo(Guid id, bool bloquear, CancellationToken ct)
    {
        if (bloquear && id == UsuarioId) return Conflict(new { message = "No puedes suspender tu propia cuenta." });
        var usuario = await users.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();
        usuario.LockoutEnabled = true;
        usuario.LockoutEnd = bloquear ? DateTimeOffset.MaxValue : null;
        if (!bloquear) usuario.AccessFailedCount = 0;
        var result = await users.UpdateAsync(usuario);
        if (!result.Succeeded) return Conflict(new { message = "No fue posible actualizar la cuenta." });
        if (bloquear) await users.UpdateSecurityStampAsync(usuario);
        return NoContent();
    }

    private async Task<List<UsuarioAdminDto>> ProyectarUsuarios(IQueryable<ApplicationUser> query, CancellationToken ct)
    {
        var ahora = DateTimeOffset.UtcNow;
        var filas = await query.Select(u => new
        {
            u.Id, u.Nombre, u.Email, u.CarreraId, Carrera = u.Carrera != null ? u.Carrera.Nombre : null,
            u.Semestre, u.FechaRegistro, Bloqueado = u.LockoutEnd != null && u.LockoutEnd > ahora,
            Resenas = u.Resenas.Count, Reportes = u.Reporteusuarios.Count,
            Negocios = db.Lugars.Count(l => l.PropietarioId == u.Id)
        }).ToListAsync(ct);
        var ids = filas.Select(f => f.Id).ToList();
        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, r.Name }).ToListAsync(ct);
        return filas.Select(f => new UsuarioAdminDto(f.Id, f.Nombre, f.Email, f.CarreraId, f.Carrera, f.Semestre,
            f.FechaRegistro, roles.Where(r => r.UserId == f.Id).Select(r => r.Name!).OrderBy(r => r).ToList(),
            f.Bloqueado, f.Resenas, f.Reportes, f.Negocios)).ToList();
    }

    // ---------- Reseñas ----------

    [HttpGet("resenas")]
    [ProducesResponseType(typeof(PaginaDto<ResenaAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resenas([FromQuery] string? q, [FromQuery] Guid? lugarId,
        [FromQuery] short? calificacion, [FromQuery] bool? conReportes,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        var query = db.Resenas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(r => (r.Comentario != null && EF.Functions.ILike(r.Comentario, patron)) ||
                                     EF.Functions.ILike(r.Lugar.Nombre, patron) ||
                                     EF.Functions.ILike(r.Usuario.Nombre, patron));
        }
        if (lugarId.HasValue) query = query.Where(r => r.LugarId == lugarId);
        if (calificacion.HasValue) query = query.Where(r => r.Calificacion == calificacion);
        if (conReportes == true) query = query.Where(r => r.Reporteusuarios.Any());

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.Reporteusuarios.Count(x => x.Estado == EstadoReporte.Pendiente))
            .ThenByDescending(r => r.FechaCreacion).ThenBy(r => r.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(r => new ResenaAdminDto(r.Id, r.UsuarioId, r.Usuario.Nombre, r.Usuario.Email, r.LugarId,
                r.Lugar.Nombre, r.Calificacion, r.Comentario, r.FechaCreacion,
                r.Reporteusuarios.Count(x => x.Estado == EstadoReporte.Pendiente), r.Reporteusuarios.Count))
            .ToListAsync(ct);
        return Ok(new PaginaDto<ResenaAdminDto>(items, total, pagina, tamanoPagina));
    }

    // Moderación: retirar la reseña cierra también los reportes que apuntaban a ella.
    [HttpDelete("resenas/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarResena(Guid id, CancellationToken ct)
    {
        var resena = await db.Resenas.Include(r => r.Reporteusuarios).SingleOrDefaultAsync(r => r.Id == id, ct);
        if (resena is null) return NotFound();
        db.Reporteusuarios.RemoveRange(resena.Reporteusuarios);
        db.Resenas.Remove(resena);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "La reseña cambió. Actualiza la lista." }); }
        return NoContent();
    }

    // ---------- Negocios ----------

    [HttpGet("negocios")]
    [ProducesResponseType(typeof(IEnumerable<NegocioAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Negocios([FromQuery] string? estado, CancellationToken ct)
    {
        var query = db.Lugars.AsNoTracking().Include(l => l.Categoria).Where(l => l.PropietarioId != null);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(l => l.EstadoSolicitud == estado);
        var lugares = await query.OrderByDescending(l => l.FechaCreacion).ToListAsync(ct);
        var ids = lugares.Select(l => l.PropietarioId!.Value).Distinct().ToList();
        var duenos = await db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Email }).ToDictionaryAsync(u => u.Id, ct);
        return Ok(lugares.Select(l =>
        {
            duenos.TryGetValue(l.PropietarioId!.Value, out var d);
            return new NegocioAdminDto(l.Id, l.Nombre, l.Descripcion, l.Categoria.Nombre, l.PropietarioId,
                d?.Nombre, d?.Email, l.EstadoSolicitud, l.Activo, l.EsAmbulante, l.Ubicacion.Y, l.Ubicacion.X, l.FechaCreacion);
        }));
    }

    // ---------- Carreras ----------

    [HttpGet("carreras")]
    [ProducesResponseType(typeof(IEnumerable<CarreraAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Carreras(CancellationToken ct) =>
        Ok(await db.Carreras.AsNoTracking().OrderBy(c => c.Nombre)
            .Select(c => new CarreraAdminDto(c.Id, c.Nombre, c.Facultad, c.Usuarios.Count)).ToListAsync(ct));

    [HttpPost("carreras")]
    [ProducesResponseType(typeof(CarreraAdminDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearCarrera(GuardarCarreraRequest request, CancellationToken ct)
    {
        var carrera = new Carrera { Id = Guid.NewGuid(), Nombre = request.Nombre.Trim(), Facultad = Limpiar(request.Facultad) };
        db.Carreras.Add(carrera);
        await db.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new CarreraAdminDto(carrera.Id, carrera.Nombre, carrera.Facultad, 0));
    }

    [HttpPut("carreras/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EditarCarrera(Guid id, GuardarCarreraRequest request, CancellationToken ct)
    {
        var carrera = await db.Carreras.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (carrera is null) return NotFound();
        carrera.Nombre = request.Nombre.Trim();
        carrera.Facultad = Limpiar(request.Facultad);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("carreras/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarCarrera(Guid id, CancellationToken ct)
    {
        var carrera = await db.Carreras.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (carrera is null) return NotFound();
        if (await db.Users.AnyAsync(u => u.CarreraId == id, ct))
            return Conflict(new { message = "La carrera tiene estudiantes registrados." });
        db.Carreras.Remove(carrera);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict(new { message = "La carrera tiene estudiantes registrados." }); }
        return NoContent();
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
