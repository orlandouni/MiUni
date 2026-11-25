using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.UnitTests;

// Módulo 4: Horario de clases y profesores (consultas del horario propio y enlaces a BuhoRater).
[TestClass]
public class HorarioProfesoresTests
{
    // 25-nov-2026 17:00 UTC = miércoles 10:00 en Hermosillo (UTC-7).
    private static readonly DateTime AhoraUtc = new(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc);

    private static HorarioClase Clase(Guid usuario, string materia, short dia, string inicio, string fin,
        string? salon = null, string? profesor = null) => new()
    {
        Id = Guid.NewGuid(), UsuarioId = usuario, Materia = materia, Dia = dia,
        HoraInicio = TimeOnly.Parse(inicio), HoraFin = TimeOnly.Parse(fin), Salon = salon, Profesor = profesor
    };

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("\"¿Qué clases tengo hoy?\" un miércoles lista solo las clases del miércoles.")]
    public async Task Horario_ClasesDeHoy_ListaSoloLasDelDia()
    {
        await using var db = Apoyo.BaseEnMemoria();
        var alumno = Guid.NewGuid();
        db.HorarioClases.AddRange(
            Clase(alumno, "Cálculo I", 3, "07:00", "08:00", "5K-201", "SOTO MORA CLAUDIA"),
            Clase(alumno, "Física I", 4, "09:00", "10:00"));
        await db.SaveChangesAsync();

        var r = await PersonalSchedule.Resolve(db, alumno, "¿Qué clases tengo hoy?", AhoraUtc, default);

        StringAssert.Contains(r.Answer, "Clases guardadas para miercoles");
        StringAssert.Contains(r.Answer, "Cálculo I; salón 5K-201; profesor SOTO MORA CLAUDIA");
        Assert.IsFalse(r.Answer!.Contains("Física I"));
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Si el alumno no tiene horario guardado, se le indica dónde capturarlo.")]
    public async Task Horario_AlumnoSinHorario_LeIndicaCapturarlo()
    {
        await using var db = Apoyo.BaseEnMemoria();

        var r = await PersonalSchedule.Resolve(db, Guid.NewGuid(), "mi horario", AhoraUtc, default);

        StringAssert.StartsWith(r.Answer, "No tienes un horario guardado.");
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("A las 10:00 en punto, la clase de 10:00 a 11:00 ya es \"ahora\" y la que terminó a las 10:00 ya no.")]
    public async Task Horario_ClaseActual_EnElCambioDeHora()
    {
        await using var db = Apoyo.BaseEnMemoria();
        var alumno = Guid.NewGuid();
        db.HorarioClases.AddRange(
            Clase(alumno, "Álgebra", 3, "09:00", "10:00"),
            Clase(alumno, "Programación", 3, "10:00", "11:00"));
        await db.SaveChangesAsync();

        var r = await PersonalSchedule.Resolve(db, alumno, "¿Qué clase tengo ahora?", AhoraUtc, default);

        StringAssert.Contains(r.Answer, "Programación");
        Assert.IsFalse(r.Answer!.Contains("Álgebra"));
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("El horario es semanal: preguntar por festivos o cancelaciones aclara que eso no se guarda.")]
    public async Task Horario_PreguntaPorFestivos_AclaraQueNoSeGuardan()
    {
        await using var db = Apoyo.BaseEnMemoria();

        var r = await PersonalSchedule.Resolve(db, Guid.NewGuid(), "¿Tengo clase en mi horario el día festivo?", AhoraUtc, default);

        StringAssert.Contains(r.Answer, "sin periodos, festivos ni cancelaciones");
    }

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("Un enlace de perfil de BuhoRater válido se acepta y se extrae su id.")]
    public void BuhoRater_EnlaceDePerfilValido_SeAcepta()
    {
        var ok = BuhoRater.TryLeerPerfil("https://www.buhorater.com/perfil?id=3f2504e0-4f89-11d3-9a0c-0305e82c3301", out var id);

        Assert.IsTrue(ok);
        Assert.AreEqual(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"), id);
    }

    [DataTestMethod, TestCategory(Caso.Error)]
    [Description("Enlaces de otro sitio, de otra página o sin id válido se rechazan.")]
    [DataRow("https://www.buhorater.com.evil.com/perfil?id=3f2504e0-4f89-11d3-9a0c-0305e82c3301", DisplayName = "Dominio falso")]
    [DataRow("https://www.buhorater.com/buscar?id=3f2504e0-4f89-11d3-9a0c-0305e82c3301", DisplayName = "Otra página")]
    [DataRow("https://www.buhorater.com/perfil?id=no-es-un-id", DisplayName = "Id inválido")]
    [DataRow("ftp://buhorater.com/perfil?id=3f2504e0-4f89-11d3-9a0c-0305e82c3301", DisplayName = "Protocolo FTP")]
    public void BuhoRater_EnlaceInvalido_SeRechaza(string enlace)
    {
        Assert.IsFalse(BuhoRater.TryLeerPerfil(enlace, out _));
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("Un id vacío (todo ceros) en el enlace no cuenta como perfil; un nombre vacío no genera búsqueda.")]
    public void BuhoRater_IdEnCerosYNombreVacio_SeRechazan()
    {
        Assert.IsFalse(BuhoRater.TryLeerPerfil($"https://buhorater.com/perfil?id={Guid.Empty}", out _));
        Assert.IsNull(BuhoRater.UrlBusqueda("   "));
        Assert.IsNull(BuhoRater.Clave(null));
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("El mismo profe se reconoce aunque el horario ponga primero los apellidos y sin acentos.")]
    public void BuhoRater_MismoProfesorEscritoDistinto_TieneLaMismaClave()
    {
        var delHorario = BuhoRater.Clave("SOTO MORA CLAUDIA PAULINA");
        var deBuhoRater = BuhoRater.Clave("Claudia Paulina  Soto Mora");

        Assert.AreEqual(delHorario, deBuhoRater);
        Assert.AreEqual(BuhoRater.Clave("José Pérez"), BuhoRater.Clave("PEREZ JOSE"));
    }
}
