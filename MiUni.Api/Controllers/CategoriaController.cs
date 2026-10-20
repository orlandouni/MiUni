// Controllers/CategoriaController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;
using MiUni.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using MiUni.Api.Identity;
using Npgsql;

namespace MiUni.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController : ControllerBase
{
    private readonly MiUniDbContext _context;

    public CategoriaController(MiUniDbContext context)
    {
        _context = context;
    }

  [HttpGet]
public async Task<IActionResult> GetAll()
{
    var categorias = await _context.Categoria
        .AsNoTracking()
        .OrderBy(c => c.Nombre)
        .Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, Icono = c.Icono })
        .ToListAsync();
    return Ok(categorias);
}

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var categoria = await _context.Categoria.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        return categoria is null ? NotFound() : Ok(Dto(categoria));
    }

    [HttpPost, Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarCategoriaRequest request, CancellationToken ct)
    {
        var categoria = new Categorium { Id = Guid.NewGuid(), Nombre = request.Nombre.Trim(), Icono = request.Icono?.Trim() };
        _context.Categoria.Add(categoria);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = categoria.Id }, Dto(categoria));
    }

    [HttpPut("{id:guid}"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Editar(Guid id, GuardarCategoriaRequest request, CancellationToken ct)
    {
        var categoria = await _context.Categoria.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (categoria is null) return NotFound();
        categoria.Nombre = request.Nombre.Trim();
        categoria.Icono = request.Icono?.Trim();
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var categoria = await _context.Categoria.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (categoria is null) return NotFound();
        if (await _context.Lugars.AnyAsync(l => l.CategoriaId == id, ct))
            return Conflict(new { message = "La categoría tiene lugares asociados, incluidos los inactivos." });
        _context.Categoria.Remove(categoria);
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict(new { message = "La categoría tiene lugares asociados." }); }
        return NoContent();
    }

    private static CategoriaDto Dto(Categorium c) => new() { Id = c.Id, Nombre = c.Nombre, Icono = c.Icono };
}
