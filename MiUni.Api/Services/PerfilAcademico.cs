using Microsoft.EntityFrameworkCore;
using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Models;

namespace MiUni.Api.Services;

// Reúne lo que el estudiante registró en su perfil (carrera, semestre, kárdex y horario)
// para que el asistente pueda personalizar sus respuestas.
public static class PerfilAcademico
{
    private static readonly string[] Dias = ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    public static KardexResumenDto Resumir(IEnumerable<KardexMateria> materias)
    {
        var lista = materias.ToList();
        var calificadas = lista
            .Where(m => m.Calificacion.HasValue && m.Estado is EstadoMateria.Aprobada or EstadoMateria.Reprobada)
            .Select(m => m.Calificacion!.Value)
            .ToList();
        return new KardexResumenDto(
            lista.Where(m => m.Estado == EstadoMateria.Aprobada).Sum(m => m.Creditos),
            calificadas.Count == 0 ? null : Math.Round(calificadas.Average(), 2),
            lista.Count(m => m.Estado == EstadoMateria.Aprobada),
            lista.Count(m => m.Estado == EstadoMateria.Reprobada),
            lista.Count(m => m.Estado == EstadoMateria.Cursando));
    }

    public static async Task<object?> ConstruirAsync(MiUniDbContext context, Guid usuarioId, CancellationToken ct)
    {
        var usuario = await context.Users.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new
            {
                u.Nombre,
                Carrera = u.Carrera == null ? null : u.Carrera.Nombre,
                Facultad = u.Carrera == null ? null : u.Carrera.Facultad,
                u.Semestre
            })
            .SingleOrDefaultAsync(ct);
        if (usuario is null) return null;

        var kardex = await context.KardexMaterias.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Semestre).ThenBy(m => m.Materia)
            .ToListAsync(ct);
        var horario = await context.HorarioClases.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio)
            .ToListAsync(ct);

        return new
        {
            nombre = usuario.Nombre,
            carrera = usuario.Carrera,
            facultad = usuario.Facultad,
            semestre = usuario.Semestre,
            kardex = new
            {
                resumen = Resumir(kardex),
                materias = kardex.Select(m => new
                {
                    materia = m.Materia,
                    clave = m.Clave,
                    periodo = m.Periodo,
                    semestre = m.Semestre,
                    calificacion = m.Calificacion,
                    creditos = m.Creditos,
                    estado = m.Estado.ToString()
                })
            },
            horario = horario.Select(c => new
            {
                dia = c.Dia is >= 1 and <= 7 ? Dias[c.Dia - 1] : c.Dia.ToString(),
                inicio = c.HoraInicio.ToString("HH:mm"),
                fin = c.HoraFin.ToString("HH:mm"),
                materia = c.Materia,
                profesor = c.Profesor,
                salon = c.Salon
            })
        };
    }

    // Fecha y hora del campus (Hermosillo, UTC-7 sin horario de verano) para preguntas
    // como "¿qué clase tengo ahora?".
    public static string FechaHoraCampus(DateTime utcNow)
    {
        TimeZoneInfo zona;
        try { zona = TimeZoneInfo.FindSystemTimeZoneById("America/Hermosillo"); }
        catch (Exception) { zona = TimeZoneInfo.CreateCustomTimeZone("MiUni-7", TimeSpan.FromHours(-7), "UTC-7", "UTC-7"); }
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), zona);
        var dia = Dias[((int)local.DayOfWeek + 6) % 7];
        return $"{dia} {local:yyyy-MM-dd HH:mm} (hora de Hermosillo)";
    }
}
