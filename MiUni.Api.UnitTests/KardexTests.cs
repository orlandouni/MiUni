using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Models;
using MiUni.Api.Services;

namespace MiUni.Api.UnitTests;

// Módulo 2: Kárdex académico (resumen de créditos y promedio, captura y consulta desde ChatMuni).
[TestClass]
public class KardexTests
{
    private static KardexMateria Materia(string nombre, EstadoMateria estado, decimal? calificacion, short creditos,
        Guid? usuario = null) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuario ?? Guid.NewGuid(),
        Materia = nombre,
        Estado = estado,
        Calificacion = calificacion,
        Creditos = creditos
    };

    [TestMethod, TestCategory(Caso.Normal)]
    [Description("El resumen suma créditos aprobados, promedia calificaciones y cuenta materias por estado.")]
    public void Resumen_KardexTipico_CalculaCreditosPromedioYConteos()
    {
        var materias = new[]
        {
            Materia("Cálculo I", EstadoMateria.Aprobada, 90, 10),
            Materia("Álgebra", EstadoMateria.Aprobada, 80, 8),
            Materia("Física I", EstadoMateria.Reprobada, 40, 8),
            Materia("Programación", EstadoMateria.Cursando, null, 9)
        };

        var r = PerfilAcademico.Resumir(materias);

        Assert.AreEqual(18, r.CreditosAprobados, "Solo cuentan los créditos de materias aprobadas.");
        Assert.AreEqual(70m, r.Promedio, "Promedio de 90, 80 y 40.");
        Assert.AreEqual(2, r.MateriasAprobadas);
        Assert.AreEqual(1, r.MateriasReprobadas);
        Assert.AreEqual(1, r.MateriasCursando);
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("Un kárdex vacío da cero créditos y ningún promedio (no truena al dividir entre cero).")]
    public void Resumen_KardexVacio_SinPromedioYEnCeros()
    {
        var r = PerfilAcademico.Resumir([]);

        Assert.AreEqual(0, r.CreditosAprobados);
        Assert.IsNull(r.Promedio);
        Assert.AreEqual(0, r.MateriasAprobadas + r.MateriasReprobadas + r.MateriasCursando);
    }

    [TestMethod, TestCategory(Caso.Limite)]
    [Description("El promedio se redondea a 2 decimales.")]
    public void Resumen_PromedioConDecimales_SeRedondeaADos()
    {
        var r = PerfilAcademico.Resumir([
            Materia("A", EstadoMateria.Aprobada, 100, 5),
            Materia("B", EstadoMateria.Aprobada, 90, 5),
            Materia("C", EstadoMateria.Aprobada, 95.5m, 5)
        ]);

        Assert.AreEqual(95.17m, r.Promedio);
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("Las materias dadas de baja o en curso no cuentan para el promedio ni los créditos.")]
    public void Resumen_BajasYMateriasEnCurso_NoAfectanElPromedio()
    {
        var r = PerfilAcademico.Resumir([
            Materia("Cálculo I", EstadoMateria.Aprobada, 100, 10),
            Materia("Química", EstadoMateria.Baja, 0, 8),
            Materia("Programación", EstadoMateria.Cursando, 30, 9)
        ]);

        Assert.AreEqual(100m, r.Promedio);
        Assert.AreEqual(10, r.CreditosAprobados);
    }

    [DataTestMethod, TestCategory(Caso.Limite)]
    [Description("La calificación capturada debe estar entre 0 y 100.")]
    [DataRow(-1.0, false, DisplayName = "Calificación -1: rechazada")]
    [DataRow(0.0, true, DisplayName = "Calificación 0: aceptada")]
    [DataRow(100.0, true, DisplayName = "Calificación 100: aceptada")]
    [DataRow(101.0, false, DisplayName = "Calificación 101: rechazada")]
    public void Captura_RangoDeCalificacion(double calificacion, bool esperadoValido)
    {
        var request = new GuardarKardexMateriaRequest
        {
            Materia = "Cálculo I", Creditos = 10, Estado = EstadoMateria.Aprobada,
            Calificacion = (decimal)calificacion
        };

        Assert.AreEqual(esperadoValido, Apoyo.Validar(request).Count == 0);
    }

    // Esta prueba FALLA a propósito: documenta un defecto real que encontraron estas pruebas.
    // [Range(0, 100)] compara como entero, así que 100.4 se redondea a 100 y se acepta.
    // Arreglo sugerido en KardexDto.cs: [Range(typeof(decimal), "0", "100", ...)].
    [DataTestMethod, TestCategory(Caso.Limite), TestCategory(Caso.Defecto)]
    [Description("DEFECTO: una calificación con decimales arriba de 100 debería rechazarse y hoy se acepta.")]
    [DataRow(100.4, DisplayName = "Calificación 100.4: debería rechazarse")]
    public void Captura_CalificacionConDecimalesArribaDe100_DeberiaRechazarse(double calificacion)
    {
        var request = new GuardarKardexMateriaRequest
        {
            Materia = "Cálculo I", Creditos = 10, Estado = EstadoMateria.Aprobada,
            Calificacion = (decimal)calificacion
        };

        Assert.AreNotEqual(0, Apoyo.Validar(request).Count, "Resultado esperado: error de validación por pasar de 100.");
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("No se puede guardar una materia sin nombre ni con más de 30 créditos.")]
    public void Captura_SinNombreYConDemasiadosCreditos_DaDosErrores()
    {
        var request = new GuardarKardexMateriaRequest { Materia = "", Creditos = 31 };

        var mensajes = Apoyo.Validar(request).Select(e => e.ErrorMessage).ToList();

        CollectionAssert.Contains(mensajes, "Escribe el nombre de la materia.");
        CollectionAssert.Contains(mensajes, "Los créditos deben estar entre 0 y 30.");
    }

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Si el alumno no ha subido su kárdex, ChatMuni le dice que lo suba desde su perfil.")]
    public async Task Chat_AlumnoSinKardex_LeIndicaSubirlo()
    {
        await using var db = Apoyo.BaseEnMemoria();

        var respuesta = await PersonalKardex.Resolve(db, Guid.NewGuid(), "¿Cuál es mi promedio?", default);

        Assert.IsNotNull(respuesta);
        StringAssert.Contains(respuesta, "No tienes materias guardadas en tu kárdex");
    }

    [TestMethod, TestCategory(Caso.Negocio)]
    [Description("ChatMuni solo usa el kárdex del propio alumno, nunca el de otro usuario.")]
    public async Task Chat_ConsultaDeCreditos_SoloUsaElKardexPropio()
    {
        await using var db = Apoyo.BaseEnMemoria();
        var alumno = Guid.NewGuid();
        db.KardexMaterias.AddRange(
            Materia("Cálculo I", EstadoMateria.Aprobada, 90, 10, alumno),
            Materia("MATERIA DE OTRO ALUMNO", EstadoMateria.Aprobada, 100, 30));
        await db.SaveChangesAsync();

        var respuesta = await PersonalKardex.Resolve(db, alumno, "¿Cuántos créditos tengo?", default);

        StringAssert.Contains(respuesta, "Créditos aprobados: 10");
        Assert.IsFalse(respuesta!.Contains("MATERIA DE OTRO ALUMNO"));
    }

    [DataTestMethod, TestCategory(Caso.Negocio)]
    [Description("Los trámites sobre el kárdex (descargarlo, solicitarlo) los contesta el RAG, no el kárdex guardado.")]
    [DataRow("¿Cómo descargo mi kárdex?")]
    [DataRow("¿Dónde solicito mi kárdex oficial?")]
    [DataRow("¿Dónde está la biblioteca?")]
    public async Task Chat_PreguntasQueNoSonDelKardexPropio_NoSeContestanAqui(string pregunta)
    {
        await using var db = Apoyo.BaseEnMemoria();

        Assert.IsNull(await PersonalKardex.Resolve(db, Guid.NewGuid(), pregunta, default));
    }
}
