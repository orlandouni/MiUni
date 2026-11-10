using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.Controllers;

// Kárdex del estudiante que inició sesión: materias, calificaciones y créditos.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KardexController(MiUniDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var materias = await context.KardexMaterias.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Semestre).ThenBy(m => m.Materia)
            .ToListAsync(ct);
        return Ok(new KardexDto(materias.Select(ToDto).ToList(), PerfilAcademico.Resumir(materias)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var materia = await context.KardexMaterias.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId, ct);
        return materia is null ? NotFound() : Ok(ToDto(materia));
    }

    [HttpPost]
    public async Task<IActionResult> Create(GuardarKardexMateriaRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (Validar(request) is { } error) return error;
        var materia = new KardexMateria { Id = Guid.NewGuid(), UsuarioId = usuarioId };
        Aplicar(materia, request);
        context.KardexMaterias.Add(materia);
        await context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = materia.Id }, ToDto(materia));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, GuardarKardexMateriaRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var materia = await context.KardexMaterias.SingleOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId, ct);
        if (materia is null) return NotFound();
        if (Validar(request) is { } error) return error;
        Aplicar(materia, request);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var materia = await context.KardexMaterias.SingleOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId, ct);
        if (materia is null) return NotFound();
        context.KardexMaterias.Remove(materia);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    private BadRequestObjectResult? Validar(GuardarKardexMateriaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Materia))
            return BadRequest(new { message = "Escribe el nombre de la materia." });
        if (!Enum.IsDefined(request.Estado))
            return BadRequest(new { message = "El estado de la materia no es válido." });
        if (request.Estado is EstadoMateria.Aprobada or EstadoMateria.Reprobada && request.Calificacion is null)
            return BadRequest(new { message = "Escribe la calificación final de la materia." });
        return null;
    }

    private static void Aplicar(KardexMateria materia, GuardarKardexMateriaRequest request)
    {
        materia.Materia = Limpiar(request.Materia)!;
        materia.Clave = Limpiar(request.Clave);
        materia.Periodo = Limpiar(request.Periodo);
        materia.Semestre = request.Semestre;
        materia.Calificacion = request.Calificacion;
        materia.Creditos = request.Creditos;
        materia.Estado = request.Estado;
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 ? null : limpio;
    }

    private static KardexMateriaDto ToDto(KardexMateria m) => new(m.Id, m.Materia, m.Clave, m.Periodo,
        m.Semestre, m.Calificacion, m.Creditos, m.Estado);

    private bool TryGetUsuario(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
