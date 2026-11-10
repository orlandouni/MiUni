using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.Controllers;

// Horario de clases del estudiante que inició sesión. Cada clase incluye el enlace
// para buscar a su profesor en BuhoRater.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HorarioController(MiUniDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clases = await context.HorarioClases.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio).ThenBy(c => c.Materia)
            .ToListAsync(ct);
        return Ok(clases.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clase = await context.HorarioClases.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        return clase is null ? NotFound() : Ok(ToDto(clase));
    }

    // Profesores distintos del horario, con su enlace a BuhoRater.
    [HttpGet("profesores")]
    public async Task<IActionResult> GetProfesores(CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clases = await context.HorarioClases.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Profesor != null)
            .Select(c => new { c.Profesor, c.Materia })
            .ToListAsync(ct);
        var profesores = clases
            .GroupBy(c => c.Profesor!, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new
            {
                Profesor = g.First().Profesor,
                Materias = g.Select(c => c.Materia).Distinct(StringComparer.CurrentCultureIgnoreCase).Order().ToList(),
                BuhoRaterUrl = BuhoRater.UrlBusqueda(g.Key),
            });
        return Ok(profesores);
    }

    [HttpPost]
    public async Task<IActionResult> Create(GuardarHorarioClaseRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (Validar(request) is { } error) return error;
        var clase = new HorarioClase { Id = Guid.NewGuid(), UsuarioId = usuarioId };
        Aplicar(clase, request);
        context.HorarioClases.Add(clase);
        await context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = clase.Id }, ToDto(clase));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, GuardarHorarioClaseRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clase = await context.HorarioClases.SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (clase is null) return NotFound();
        if (Validar(request) is { } error) return error;
        Aplicar(clase, request);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clase = await context.HorarioClases.SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        if (clase is null) return NotFound();
        context.HorarioClases.Remove(clase);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }

    private BadRequestObjectResult? Validar(GuardarHorarioClaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Materia))
            return BadRequest(new { message = "Escribe el nombre de la materia." });
        if (request.HoraFin <= request.HoraInicio)
            return BadRequest(new { message = "La hora de fin debe ser después de la hora de inicio." });
        return null;
    }

    private static void Aplicar(HorarioClase clase, GuardarHorarioClaseRequest request)
    {
        clase.Materia = Limpiar(request.Materia)!;
        clase.Profesor = Limpiar(request.Profesor);
        clase.Dia = request.Dia;
        clase.HoraInicio = request.HoraInicio;
        clase.HoraFin = request.HoraFin;
        clase.Salon = Limpiar(request.Salon);
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 ? null : limpio;
    }

    private static HorarioClaseDto ToDto(HorarioClase c) => new(c.Id, c.Materia, c.Profesor, c.Dia,
        c.HoraInicio, c.HoraFin, c.Salon, BuhoRater.UrlBusqueda(c.Profesor));

    private bool TryGetUsuario(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
