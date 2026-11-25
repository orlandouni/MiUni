using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.UnitTests;

// Módulo 5: ChatMuni (historial que se manda al asistente y detección de preguntas personales).
[TestClass]
public class ChatMuniTests
{
    private static Historialchat Mensaje(string rol, string contenido) => new()
    {
        Id = Guid.NewGuid(), Rol = rol, Contenido = contenido, FechaCreacion = DateTime.UtcNow
    };

    private static IEnumerable<Historialchat> Turnos(int cuantos) => Enumerable.Range(1, cuantos)
        .SelectMany(i => new[] { Mensaje("user", $"pregunta {i}"), Mensaje("assistant", $"respuesta {i}") });

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Dos turnos completos se mandan en orden: pregunta, respuesta, pregunta, respuesta.")]
    public void Historial_TurnosCompletos_SeConservanEnOrden()
    {
        var historial = ChatContext.CompletedTurns(Turnos(2));

        CollectionAssert.AreEqual(
            new[] { "pregunta 1", "respuesta 1", "pregunta 2", "respuesta 2" },
            historial.Select(m => m.Content).ToArray());
        CollectionAssert.AreEqual(new[] { "user", "assistant", "user", "assistant" },
            historial.Select(m => m.Role).ToArray());
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Una pregunta que quedó sin respuesta (por un error) no se manda al asistente.")]
    public void Historial_PreguntaSinRespuesta_SeDescarta()
    {
        var filas = new[]
        {
            Mensaje("user", "pregunta que falló"),
            Mensaje("user", "pregunta buena"),
            Mensaje("assistant", "respuesta buena"),
            Mensaje("user", "pregunta en curso")
        };

        var historial = ChatContext.CompletedTurns(filas);

        CollectionAssert.AreEqual(new[] { "pregunta buena", "respuesta buena" },
            historial.Select(m => m.Content).ToArray());
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("Con 10 turnos solo se mandan los 6 más recientes (máximo 12 mensajes).")]
    public void Historial_MuchosTurnos_SeRecortaAlMaximoDeMensajes()
    {
        var historial = ChatContext.CompletedTurns(Turnos(10));

        Assert.AreEqual(ChatContext.MaxMessages, historial.Count);
        Assert.AreEqual("pregunta 5", historial[0].Content);
        Assert.AreEqual("respuesta 10", historial[^1].Content);
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("Un turno que rebasa el presupuesto de 4000 bytes deja fuera ese turno y los anteriores.")]
    public void Historial_TurnoDemasiadoLargo_CortaPorTamano()
    {
        var filas = new[]
        {
            Mensaje("user", "viejo"), Mensaje("assistant", "ok"),
            Mensaje("user", new string('x', 3990)), Mensaje("assistant", "respuesta larga"),
            Mensaje("user", "reciente"), Mensaje("assistant", "ok")
        };

        var historial = ChatContext.CompletedTurns(filas);

        CollectionAssert.AreEqual(new[] { "reciente", "ok" }, historial.Select(m => m.Content).ToArray());
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Aunque se pida menos de 2 mensajes, siempre se conserva al menos el último turno completo.")]
    public void Historial_LimiteMenorADos_SeAjustaAUnTurno()
    {
        var historial = ChatContext.CompletedTurns(Turnos(3), maxMessages: 0);

        Assert.AreEqual(2, historial.Count);
        Assert.AreEqual("pregunta 3", historial[0].Content);
    }

    [DataTestMethod, TestCategory(Caso.Negocio)]
    [Description("Las preguntas sobre el horario o kárdex propio se contestan en la API y no viajan al RAG.")]
    [DataRow("¿Qué clases tengo mañana?", true, false, DisplayName = "Horario: clases de mañana")]
    [DataRow("¿Cuál es mi próxima clase?", true, false, DisplayName = "Horario: próxima clase")]
    [DataRow("¿Cuántos créditos llevo?", false, true, DisplayName = "Kárdex: créditos")]
    [DataRow("¿Qué materias reprobé?", false, true, DisplayName = "Kárdex: reprobadas")]
    [DataRow("¿Cuáles son los requisitos de titulación?", false, false, DisplayName = "Institucional: va al RAG")]
    public void Pregunta_SeClasificaComoPersonalONo(string pregunta, bool esHorario, bool esKardex)
    {
        Assert.AreEqual(esHorario, PersonalSchedule.IsPersonal(pregunta), "¿Es del horario propio?");
        Assert.AreEqual(esKardex, PersonalKardex.IsPersonal(pregunta), "¿Es del kárdex propio?");
    }

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("La hora del campus es la de Hermosillo (UTC-7, sin horario de verano).")]
    public void FechaHoraCampus_ConvierteUtcAHermosillo()
    {
        var texto = PerfilAcademico.FechaHoraCampus(new DateTime(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual("miércoles 2026-11-25 10:00 (hora de Hermosillo)", texto);
    }
}
