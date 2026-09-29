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
   [HttpGet]
public async Task<IActionResult> GetAll([FromQuery] string? categoria)
{
    var query = _context.Lugars
        .Include(l => l.Categoria)
        .Where(l => l.Activo);

    if (!string.IsNullOrWhiteSpace(categoria))
    {
        query = query.Where(l => l.Categoria.Nombre == categoria);
    }

    // Primero traemos las entidades completas (aquí SÍ se ejecuta el SQL)
    var lugaresEntidades = await query.ToListAsync();

    // Y luego mapeamos a DTO en memoria (esto ya no es SQL, es C# puro)
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