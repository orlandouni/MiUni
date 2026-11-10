using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.Controllers;

// Horario de clases del estudiante que inició sesión. Cada clase incluye el enlace
// a BuhoRater de su profesor: su perfil si algún estudiante ya lo vinculó, o una búsqueda.
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
        var perfiles = await Perfiles(clases.Select(c => c.Profesor), ct);
        return Ok(clases.Select(c => ToDto(c, perfiles)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clase = await context.HorarioClases.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);
        return clase is null ? NotFound() : Ok(ToDto(clase, await Perfiles([clase.Profesor], ct)));
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
        var perfiles = await Perfiles(clases.Select(c => c.Profesor), ct);
        var profesores = clases
            .GroupBy(c => c.Profesor!, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new
            {
                Profesor = g.First().Profesor,
                Materias = g.Select(c => c.Materia).Distinct(StringComparer.CurrentCultureIgnoreCase).Order().ToList(),
                BuhoRaterUrl = BuhoRater.Url(g.Key, perfiles),
                BuhoRaterPerfil = BuhoRater.TienePerfil(g.Key, perfiles),
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
        return CreatedAtAction(nameof(GetById), new { id = clase.Id }, ToDto(clase, await Perfiles([clase.Profesor], ct)));
    }

    public const long TamanoMaximoPdf = 5 * 1024 * 1024;

    // Lee el PDF del horario y devuelve las clases encontradas, sin guardarlas: el
    // estudiante las revisa y confirma con PUT /api/Horario.
    [HttpPost("importar")]
    [RequestSizeLimit(TamanoMaximoPdf + 64 * 1024)]
    public async Task<IActionResult> Importar(IFormFile? archivo, CancellationToken ct)
    {
        if (!TryGetUsuario(out _)) return Unauthorized();
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Sube el PDF de tu horario." });
        if (archivo.Length > TamanoMaximoPdf)
            return BadRequest(new { message = "El PDF no puede pasar de 5 MB." });

        await using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        if (!memoria.GetBuffer().AsSpan(0, (int)Math.Min(5, memoria.Length)).SequenceEqual("%PDF-"u8))
            return BadRequest(new { message = "El archivo no es un PDF." });
        memoria.Position = 0;

        IReadOnlyList<ClaseImportada> clases;
        try { clases = HorarioPdfParser.Leer(memoria); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new { message = "No pudimos abrir el PDF. Revisa que no esté dañado ni protegido con contraseña." });
        }
        if (clases.Count == 0)
            return UnprocessableEntity(new { message = "No encontramos clases en el PDF. Revisa que sea el horario del portal o captúralo a mano." });

        var perfiles = await Perfiles(clases.Select(c => c.Profesor), ct);
        return Ok(clases.Select(c => new ClaseImportadaDto(c.Materia, c.Profesor, c.Dia, c.HoraInicio,
            c.HoraFin, c.Salon, BuhoRater.Url(c.Profesor, perfiles), BuhoRater.TienePerfil(c.Profesor, perfiles))));
    }

    // Reemplaza todo el horario (por ejemplo, al confirmar lo leído del PDF).
    [HttpPut]
    public async Task<IActionResult> Reemplazar(List<GuardarHorarioClaseRequest> clases, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (!await context.Users.AnyAsync(u => u.Id == usuarioId, ct)) return Unauthorized();
        if (clases.Count > 100) return BadRequest(new { message = "El horario no puede tener más de 100 clases." });
        foreach (var request in clases)
            if (Validar(request) is { } error) return error;

        var actuales = await context.HorarioClases.Where(c => c.UsuarioId == usuarioId).ToListAsync(ct);
        context.HorarioClases.RemoveRange(actuales);
        var nuevas = clases.Select(request =>
        {
            var clase = new HorarioClase { Id = Guid.NewGuid(), UsuarioId = usuarioId };
            Aplicar(clase, request);
            return clase;
        }).ToList();
        context.HorarioClases.AddRange(nuevas);
        await context.SaveChangesAsync(ct);
        var perfiles = await Perfiles(nuevas.Select(c => c.Profesor), ct);
        return Ok(nuevas.OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio).Select(c => ToDto(c, perfiles)));
    }

    // Guarda (o quita, con el enlace vacío) el perfil de BuhoRater de un profe. Solo lo
    // cargan los administradores; los estudiantes solo ven el botón que lleva directo al perfil.
    [HttpPut("profesores/buhorater"), Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> VincularBuhoRater(VincularBuhoRaterRequest request, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        var clave = BuhoRater.Clave(request.Profesor);
        if (clave is null) return BadRequest(new { message = "Indica el profesor." });

        // Solo profes que aparecen en algún horario, para no llenar la tabla de nombres sueltos.
        var profesores = await context.HorarioClases.AsNoTracking()
            .Where(c => c.Profesor != null)
            .Select(c => c.Profesor!).Distinct().ToListAsync(ct);
        var nombre = profesores.FirstOrDefault(p => BuhoRater.Clave(p) == clave);
        if (nombre is null) return NotFound(new { message = "Ese profesor no está en ningún horario." });

        var actual = await context.ProfesorBuhoRaters.SingleOrDefaultAsync(p => p.Clave == clave, ct);
        if (string.IsNullOrWhiteSpace(request.Enlace))
        {
            if (actual is not null) context.ProfesorBuhoRaters.Remove(actual);
        }
        else
        {
            if (!BuhoRater.TryLeerPerfil(request.Enlace, out var perfilId))
                return BadRequest(new { message = "Pega el enlace del perfil del profe en BuhoRater, por ejemplo https://www.buhorater.com/perfil?id=…" });
            actual ??= context.ProfesorBuhoRaters.Add(new ProfesorBuhoRater { Clave = clave }).Entity;
            actual.Nombre = nombre;
            actual.PerfilId = perfilId;
            actual.ActualizadoPorId = usuarioId;
            actual.FechaActualizacion = DateTime.UtcNow;
        }
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            // Otro estudiante lo vinculó al mismo tiempo; gana el que llegó primero.
            return Conflict(new { message = "Alguien acaba de vincular a este profe. Recarga para ver su enlace." });
        }

        var perfiles = await Perfiles([nombre], ct);
        return Ok(new PerfilBuhoRaterDto(nombre, BuhoRater.Url(nombre, perfiles), BuhoRater.TienePerfil(nombre, perfiles)));
    }

    // Carga de golpe los perfiles de BuhoRater desde una página guardada (.html) o un CSV.
    // Los profes que ya tenían perfil se actualizan.
    [HttpPost("profesores/buhorater/importar"), Authorize(Roles = AppRoles.Administrador)]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> ImportarBuhoRater(IFormFile? archivo, CancellationToken ct)
    {
        if (!TryGetUsuario(out var usuarioId)) return Unauthorized();
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Sube la página de BuhoRater guardada (.html) o un CSV." });
        if (archivo.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede pasar de 10 MB." });

        string contenido;
        using (var lector = new StreamReader(archivo.OpenReadStream()))
            contenido = await lector.ReadToEndAsync(ct);
        var resultado = BuhoRaterImport.Leer(contenido, archivo.FileName ?? "");
        if (resultado.Perfiles.Count == 0)
            return UnprocessableEntity(new { message = "No encontramos enlaces de perfiles de BuhoRater con el nombre del profe en ese archivo." });

        var porClave = resultado.Perfiles.ToDictionary(p => BuhoRater.Clave(p.Nombre)!, p => p);
        var claves = porClave.Keys.ToList();
        var existentes = await context.ProfesorBuhoRaters.Where(p => claves.Contains(p.Clave)).ToDictionaryAsync(p => p.Clave, ct);
        int nuevos = 0, actualizados = 0;
        foreach (var (clave, perfil) in porClave)
        {
            if (!existentes.TryGetValue(clave, out var fila))
            {
                fila = context.ProfesorBuhoRaters.Add(new ProfesorBuhoRater { Clave = clave }).Entity;
                nuevos++;
            }
            else if (fila.PerfilId != perfil.PerfilId) actualizados++;
            else continue;
            fila.Nombre = perfil.Nombre;
            fila.PerfilId = perfil.PerfilId;
            fila.ActualizadoPorId = usuarioId;
            fila.FechaActualizacion = DateTime.UtcNow;
        }
        await context.SaveChangesAsync(ct);
        return Ok(new { leidos = resultado.Perfiles.Count, nuevos, actualizados, sinNombre = resultado.SinNombre });
    }

    private async Task<IReadOnlyDictionary<string, Guid>> Perfiles(IEnumerable<string?> profesores, CancellationToken ct)
    {
        var claves = profesores.Select(BuhoRater.Clave).OfType<string>().Distinct().ToList();
        if (claves.Count == 0) return new Dictionary<string, Guid>();
        return await context.ProfesorBuhoRaters.AsNoTracking()
            .Where(p => claves.Contains(p.Clave))
            .ToDictionaryAsync(p => p.Clave, p => p.PerfilId, ct);
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
        if (request.Dia is < 1 or > 7)
            return BadRequest(new { message = "El día debe estar entre 1 (lunes) y 7 (domingo)." });
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

    private static HorarioClaseDto ToDto(HorarioClase c, IReadOnlyDictionary<string, Guid> perfiles) => new(c.Id,
        c.Materia, c.Profesor, c.Dia, c.HoraInicio, c.HoraFin, c.Salon,
        BuhoRater.Url(c.Profesor, perfiles), BuhoRater.TienePerfil(c.Profesor, perfiles));

    private bool TryGetUsuario(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
