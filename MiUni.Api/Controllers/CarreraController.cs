using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;

namespace MiUni.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarreraController(MiUniDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await context.Carreras.AsNoTracking().OrderBy(c => c.Nombre)
            .Select(c => new { c.Id, c.Nombre, c.Facultad }).ToListAsync(ct));
}
