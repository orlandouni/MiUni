using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;
using MiUni.Api.DTOs;

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
}