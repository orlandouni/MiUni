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

    // Lee el PDF del kárdex y devuelve las materias encontradas, sin guardarlas: el
    // estudiante las revisa y confirma con PUT /api/Kardex.
    [HttpPost("importar")]
    [RequestSizeLimit(HorarioController.TamanoMaximoPdf + 64 * 1024)]
    public async Task<IActionResult> Importar(IFormFile? archivo, CancellationToken ct)
    {
        if (!TryGetUsuario(out _)) return Unauthorized();
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Sube el PDF de tu kárdex." });
        if (archivo.Length > HorarioController.TamanoMaximoPdf)
            return BadRequest(new { message = "El PDF no puede pasar de 5 MB." });

        await using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        if (!memoria.GetBuffer().AsSpan(0, (int)Math.Min(5, memoria.Length)).SequenceEqual("%PDF-"u8))
            return BadRequest(new { message = "El archivo no es un PDF." });
        memoria.Position = 0;

        IReadOnlyList<MateriaImportada> materias;
        try { materias = KardexPdfParser.Leer(memoria); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new { message = "No pudimos abrir el PDF. Revisa que no esté dañado ni protegido con contraseña." });
        }
        if (materias.Count == 0)
            return UnprocessableEntity(new { message = "No encontramos materias en el PDF. Revisa que sea el kárdex del portal o captúralo a mano." });

        return Ok(materias.Select(m => new KardexMateriaImportadaDto(m.Materia, m.Clave, m.Periodo, m.Semestre,
            m.Calificacion, m.Creditos, m.Estado)));
    }

    // Reemplaza todo el kárdex (por ejemplo, al confirmar lo leído del PDF).
    [HttpPut]
    public async Task<IActionResult> Reemplazar(List<GuardarKardexMateriaRequest> materias, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (materias.Count > 200) return BadRequest(new { message = "El kárdex no puede tener más de 200 materias." });
        foreach (var request in materias)
            if (Validar(request) is { } error) return error;

        var actuales = await context.KardexMaterias.Where(m => m.UsuarioId == usuarioId).ToListAsync(ct);
        context.KardexMaterias.RemoveRange(actuales);
        var nuevas = materias.Select(request =>
        {
            var materia = new KardexMateria { Id = Guid.NewGuid(), UsuarioId = usuarioId };
            Aplicar(materia, request);
            return materia;
        }).ToList();
        context.KardexMaterias.AddRange(nuevas);
        await context.SaveChangesAsync(ct);
        var ordenadas = nuevas.OrderBy(m => m.Semestre).ThenBy(m => m.Materia).ToList();
        return Ok(new KardexDto(ordenadas.Select(ToDto).ToList(), PerfilAcademico.Resumir(ordenadas)));
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
