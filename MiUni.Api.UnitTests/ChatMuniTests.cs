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

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Las preguntas sobre el horario o kárdex propio se contestan en la API; las institucionales van al RAG.")]
    public void Pregunta_PersonalOInstitucional_SeDirigeAlModuloCorrecto()
    {
        Assert.IsTrue(PersonalSchedule.IsPersonal("¿Qué clases tengo mañana?"), "Horario propio");
        Assert.IsTrue(PersonalKardex.IsPersonal("¿Cuántos créditos llevo?"), "Kárdex propio");

        var institucional = "¿Cuáles son los requisitos de titulación?";
        Assert.IsFalse(PersonalSchedule.IsPersonal(institucional), "No es del horario: va al RAG");
        Assert.IsFalse(PersonalKardex.IsPersonal(institucional), "No es del kárdex: va al RAG");
    }

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("La hora del campus es la de Hermosillo (UTC-7, sin horario de verano).")]
    public void FechaHoraCampus_ConvierteUtcAHermosillo()
    {
        var texto = PerfilAcademico.FechaHoraCampus(new DateTime(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual("miércoles 2026-11-25 10:00 (hora de Hermosillo)", texto);
    }
}
