using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Enums;
using MiUni.Api.Models;

namespace MiUni.Api.Services;

// Contesta en la API las preguntas sobre el kárdex propio (créditos, promedio, materias
// aprobadas, reprobadas o en curso), igual que PersonalSchedule con el horario: el kárdex
// nunca se manda al servicio RAG.
public static class PersonalKardex
{
    private const string Link = "[Ver o editar mi kárdex](/perfil)";

    public static bool IsPersonal(string question)
    {
        var q = PersonalSchedule.Normalize(question);
        return Regex.IsMatch(q, @"\b(mi kardex|kardex|mi promedio|mis creditos|creditos (tengo|llevo|me faltan|acumulados|aprobados)|cuantos creditos|que promedio|mis calificaciones|mi calificacion|materias (he |ya )?(aprobadas|aprobado|reprobadas|reprobado|cursando|llevo|curso)|(he|ya) (aprobado|reprobado|cursado)|que materias (llevo|curso|estoy cursando|aprobe|repobre|reprobe|tengo)|reprobe|aprobe|mi avance)\b");
    }

    public static async Task<string?> Resolve(MiUniDbContext db, Guid user, string question, CancellationToken ct)
    {
        if (!IsPersonal(question)) return null;
        var q = PersonalSchedule.Normalize(question);
        // "Qué materias me faltan" necesita el plan de estudios: eso lo contesta el RAG.
        // Trámites sobre el kárdex (cómo descargarlo o pedirlo) también son del RAG.
        if (Regex.IsMatch(q, @"\b(descargo|descargar|solicito|solicitar|tramite|tramitar|obtengo|obtener|imprimir|costo)\b"))
            return null;
        if (Regex.IsMatch(q, @"\b(me faltan|faltan|pendientes|plan de estudios|mapa curricular)\b")
            && !Regex.IsMatch(q, @"\bcreditos\b"))
            return null;

        var materias = await db.KardexMaterias.AsNoTracking().Where(m => m.UsuarioId == user)
            .OrderBy(m => m.Semestre).ThenBy(m => m.Materia).ToListAsync(ct);
        if (materias.Count == 0)
            return $"No tienes materias guardadas en tu kárdex. Súbelo en PDF desde tu perfil. {Link}";

        var r = PerfilAcademico.Resumir(materias);
        var mx = CultureInfo.GetCultureInfo("es-MX");
        var lines = new List<string>
        {
            "Según el kárdex que subiste:",
            $"- Créditos aprobados: {r.CreditosAprobados}",
            $"- Promedio: {(r.Promedio.HasValue ? r.Promedio.Value.ToString("0.##", mx) : "sin calificaciones finales todavía")}",
            $"- Materias aprobadas: {r.MateriasAprobadas}",
            $"- Materias reprobadas: {r.MateriasReprobadas}",
            $"- Materias en curso: {r.MateriasCursando}",
        };

        EstadoMateria? listar = null;
        if (Regex.IsMatch(q, @"\b(reprobad\w*|reprobe|repobre)\b")) listar = EstadoMateria.Reprobada;
        else if (Regex.IsMatch(q, @"\b(cursando|curso|llevo|en curso)\b") && !Regex.IsMatch(q, @"\bcreditos\b"))
            listar = EstadoMateria.Cursando;
        else if (Regex.IsMatch(q, @"\b(aprobad\w*|aprobe|calificaciones|calificacion)\b")) listar = EstadoMateria.Aprobada;

        if (listar is { } estado)
        {
            var seleccion = materias.Where(m => m.Estado == estado).ToList();
            var titulo = estado switch
            {
                EstadoMateria.Reprobada => "Materias reprobadas",
                EstadoMateria.Cursando => "Materias en curso",
                _ => "Materias aprobadas"
            };
            lines.Add("");
            lines.Add(seleccion.Count == 0 ? $"{titulo}: ninguna." : $"{titulo}:");
            lines.AddRange(seleccion.Take(60).Select(m =>
                $"- {m.Materia}{(m.Calificacion.HasValue ? $": {m.Calificacion.Value.ToString("0.##", mx)}" : "")}" +
                $"{(string.IsNullOrWhiteSpace(m.Periodo) ? "" : $" ({m.Periodo})")}"));
        }

        lines.Add("");
        lines.Add($"Son los datos guardados en MiUni; el kárdex oficial está en el portal de alumnos. {Link}");
        return string.Join("\n", lines);
    }
}
