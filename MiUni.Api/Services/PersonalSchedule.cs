using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;

namespace MiUni.Api.Services;

public sealed record ScheduleChatResult(string? Answer = null, RagPersonalContext? Context = null);

public static class PersonalSchedule
{
    private static readonly string[] Days = ["lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo"];
    private const string Link = "[Ver o editar mi horario](/horario)";
    private const string Caveat = "Es tu horario semanal guardado; no confirma festivos ni cancelaciones.";

    public static Task<List<HorarioClase>> Read(MiUniDbContext db, Guid user, CancellationToken ct) =>
        db.HorarioClases.AsNoTracking().Where(c => c.UsuarioId == user)
            .OrderBy(c => c.Dia).ThenBy(c => c.HoraInicio).ThenBy(c => c.Materia).ThenBy(c => c.Id).ToListAsync(ct);

    internal static string Normalize(string value) => string.Concat(value.ToLowerInvariant()
        .Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

    public static bool IsPersonal(string question)
    {
        var q = Normalize(question);
        return Regex.IsMatch(q, @"\b(mi horario|mis clases|mi clase|que clases? (tengo|me toca)|que (tengo|me toca) (hoy|manana|el |ahora)|mi (siguiente|proxima) clase|cual es mi (siguiente|proxima) clase)\b");
    }

    public static async Task<ScheduleChatResult> Resolve(MiUniDbContext db, Guid user, string question,
        DateTime utcNow, CancellationToken ct)
    {
        if (!IsPersonal(question)) return new();
        var q = Normalize(question);
        if (Regex.IsMatch(q, @"\b(festivo|feriado|vacaciones|cancelacion|cancelada|semestre pasado|semestre anterior|semestre siguiente|proximo semestre)\b")
            || Regex.IsMatch(q, @"\b20\d{2}[-/]\d\b"))
            return new($"El módulo solo guarda un horario semanal recurrente, sin periodos, festivos ni cancelaciones. {Link}");
        var rows = await Read(db, user, ct);
        var mixed = Regex.IsMatch(q, @"\b(becas?|intercambios?|practicas?|requisitos?|tramites?|reglamentos?|documentos?|inscripcion)\b");
        if (rows.Count == 0)
            return mixed ? new(Context: new([], PerfilAcademico.FechaHoraCampus(utcNow)))
                : new($"No tienes un horario guardado. {Link}");
        var local = PerfilAcademico.CampusLocalTime(utcNow);
        var today = ((int)local.DayOfWeek + 6) % 7 + 1;
        var time = TimeOnly.FromDateTime(local);
        IEnumerable<HorarioClase> selected = rows;
        string heading = "Tu horario semanal guardado";
        var named = Array.FindIndex(Days, d => Regex.IsMatch(q, $@"\b{d}\b"));
        if (Regex.IsMatch(q, @"\b(ahora|actual)\b"))
        {
            selected = rows.Where(c => c.Dia == today && c.HoraInicio <= time && time < c.HoraFin);
            heading = "Clases guardadas para este momento";
        }
        else if (Regex.IsMatch(q, @"\b(siguiente|proxima)\b"))
        {
            var upcoming = rows.SelectMany(c => Enumerable.Range(0, 8)
                    .Where(offset => (today - 1 + offset) % 7 + 1 == c.Dia)
                    .Select(offset => (Row: c, At: local.Date.AddDays(offset).Add(c.HoraInicio.ToTimeSpan()))))
                .Where(c => c.At >= local && c.At <= local.AddDays(7)).OrderBy(c => c.At).ToList();
            selected = upcoming.Count == 0 ? [] : upcoming.Where(c => c.At == upcoming[0].At).Select(c => c.Row);
            heading = upcoming.Count == 0 ? "Próxima clase" : $"Próxima clase: {upcoming[0].At:yyyy-MM-dd}";
        }
        else if (named >= 0 || Regex.IsMatch(q, @"\b(hoy|manana)\b"))
        {
            var day = named >= 0 ? named + 1 : q.Contains("manana") ? today % 7 + 1 : today;
            selected = rows.Where(c => c.Dia == day);
            heading = $"Clases guardadas para {Days[day - 1]}";
        }
        var matching = selected.ToList();
        // Mixed institutional queries receive selected fields, never the entire academic profile.
        if (mixed)
            return new(Context: new(matching.Select(c => new RagScheduleRow(c.Materia, c.Dia,
                c.HoraInicio.ToString("HH:mm"), c.HoraFin.ToString("HH:mm"), c.Salon, c.Profesor)).ToList(),
                PerfilAcademico.FechaHoraCampus(utcNow)));
        var details = matching.Count == 0 ? "No hay clases guardadas para esa consulta." :
            string.Join("\n", matching.Select(c => $"- {Days[c.Dia - 1]} {c.HoraInicio:HH:mm}–{c.HoraFin:HH:mm}: {c.Materia}; " +
                $"{(string.IsNullOrWhiteSpace(c.Salon) ? "salón no registrado" : "salón " + c.Salon)}; " +
                $"{(string.IsNullOrWhiteSpace(c.Profesor) ? "profesor no registrado" : "profesor " + c.Profesor)}."));
        return new($"{heading}\n{details}\n{Caveat}\n{Link}");
    }
}
