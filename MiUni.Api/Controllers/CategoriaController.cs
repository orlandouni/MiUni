// Controllers/CategoriaController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;

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
    var categorias = await _context.Categoria.ToListAsync();
    return Ok(categorias);
}
}