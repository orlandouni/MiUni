using Microsoft.AspNetCore.Mvc;
using MiUni.Api.DTOs;
using MiUni.Api.Services;

namespace MiUni.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RutaController(IRutaService rutas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RutaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calcular([FromBody] RutaRequest request, CancellationToken cancellationToken)
    {
        var ruta = await rutas.CalcularAsync(request, cancellationToken);
        return ruta is null
            ? NotFound(new { mensaje = "No existe una ruta transitable entre los puntos dentro del radio de conexión." })
            : Ok(ruta);
    }
}
