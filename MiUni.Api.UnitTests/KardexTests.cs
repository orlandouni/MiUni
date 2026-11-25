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

    [TestMethod, TestCategory(Caso.Error)]
    [Description("Si el alumno no ha subido su kárdex, ChatMuni le dice que lo suba desde su perfil.")]
    public async Task Chat_AlumnoSinKardex_LeIndicaSubirlo()
    {
        await using var db = Apoyo.BaseEnMemoria();

        var respuesta = await PersonalKardex.Resolve(db, Guid.NewGuid(), "¿Cuál es mi promedio?", default);

        Assert.IsNotNull(respuesta);
        StringAssert.Contains(respuesta, "No tienes materias guardadas en tu kárdex");
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

    // Esta prueba FALLA a propósito: documenta un defecto real que encontraron estas pruebas.
    // [Range(0, 100)] compara como entero, así que 100.4 se redondea a 100 y se acepta.
    // Arreglo sugerido en KardexDto.cs: [Range(typeof(decimal), "0", "100", ...)].
    [TestMethod, TestCategory(Caso.Limite), TestCategory(Caso.Defecto)]
    [Description("DEFECTO: una calificación con decimales arriba de 100 debería rechazarse y hoy se acepta.")]
    public void Captura_CalificacionDe100Punto4_DeberiaRechazarse()
    {
        var request = new GuardarKardexMateriaRequest
        {
            Materia = "Cálculo I", Creditos = 10, Estado = EstadoMateria.Aprobada, Calificacion = 100.4m
        };

        var errores = Apoyo.Validar(request);

        Assert.AreNotEqual(0, errores.Count, "Resultado esperado: error de validación por pasar de 100.");
    }
}
