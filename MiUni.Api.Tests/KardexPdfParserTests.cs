using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Controllers;
using MiUni.Api.DTOs;
using MiUni.Api.Enums;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class KardexPdfParserTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "kardex-ejemplo.pdf");

    // Fixtures/kardex-ejemplo.pdf es un kárdex en tabla con datos inventados (ver generar_kardex_ejemplo.py).
    [Fact]
    public void ReadsKardexWithSectionsWrappedNamesAndTextGrades()
    {
        using var pdf = File.OpenRead(Fixture);
        var materias = KardexPdfParser.Leer(pdf);

        Assert.Equal(6, materias.Count);
        var calculo = Assert.Single(materias, m => m.Materia == "CÁLCULO DIFERENCIAL E INTEGRAL I");
        Assert.Equal(("04120", "2025-1", 92m, (short)10, EstadoMateria.Aprobada),
            (calculo.Clave, calculo.Periodo, calculo.Calificacion, calculo.Creditos, calculo.Estado));

        var algebra = materias.Where(m => m.Materia == "ÁLGEBRA SUPERIOR").ToList();
        Assert.Equal([EstadoMateria.Reprobada, EstadoMateria.Aprobada], algebra.Select(m => m.Estado));
        Assert.Equal(["2025-1", "2025-2"], algebra.Select(m => m.Periodo));

        var estrategias = Assert.Single(materias, m => m.Materia == "ESTRATEGIAS PARA APRENDER A APRENDER");
        Assert.Equal(EstadoMateria.Aprobada, estrategias.Estado);
        Assert.Null(estrategias.Calificacion);
        Assert.Equal(4, estrategias.Creditos);

        Assert.Equal(EstadoMateria.Baja, Assert.Single(materias, m => m.Materia == "FÍSICA I").Estado);
        var poo = Assert.Single(materias, m => m.Materia == "PROGRAMACIÓN ORIENTADA A OBJETOS II");
        Assert.Equal(EstadoMateria.Cursando, poo.Estado);
        Assert.Equal("2025-2", poo.Periodo);
        Assert.DoesNotContain(materias, m => m.Materia.Contains("NOTA") || m.Materia.Contains("documento"));
    }

    // Fixtures/kardex-unison-ejemplo.pdf copia el acomodo del "Kárdex electrónico" del portal
    // de la UNISON con datos inventados (ver generar_kardex_unison_ejemplo.py).
    [Fact]
    public void ReadsUnisonElectronicKardex()
    {
        using var pdf = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "kardex-unison-ejemplo.pdf"));
        var materias = KardexPdfParser.Leer(pdf);

        Assert.Equal(["0119", "6880", "6881", "6885", "4114", "4121", "6883"], materias.Select(m => m.Clave));
        var actividades = materias[0];
        Assert.Equal(("ACTIVIDADES CULTURALES Y DEPORTIVAS", (short)3, EstadoMateria.Aprobada, (decimal?)null),
            (actividades.Materia, actividades.Creditos, actividades.Estado, actividades.Calificacion));
        Assert.Equal((98m, "2023-2", (short?)1), (materias[1].Calificacion, materias[1].Periodo, materias[1].Semestre));
        // La regularización sustituye a la ordinaria reprobada.
        Assert.Equal((75m, EstadoMateria.Aprobada), (materias[2].Calificacion, materias[2].Estado));
        Assert.Equal((52m, EstadoMateria.Reprobada, "2024-1", (short?)2),
            (materias[3].Calificacion, materias[3].Estado, materias[3].Periodo, materias[3].Semestre));
        // En curso: el número de grupo en E2 no es calificación.
        Assert.Equal((EstadoMateria.Cursando, (decimal?)null, (short?)3, (short)6),
            (materias[5].Estado, materias[5].Calificacion, materias[5].Semestre, materias[5].Creditos));
        Assert.Equal(EstadoMateria.Baja, materias[6].Estado);
    }

    [Fact]
    public void ReadsSemesterAndPeriodColumns()
    {
        // Encabezado centrado; celdas alineadas a la izquierda.
        PalabraPdf P(string t, double x, double y) => new(t, x, t.Length * 3.5, y, 1);
        var palabras = new List<PalabraPdf>
        {
            P("Sem", 20, 500), P("Clave", 50, 500), P("Asignatura", 120, 500), P("Cr", 300, 500),
            P("Calificación", 330, 500), P("Periodo", 400, 500),
            P("1", 22, 490), P("04120", 50, 490), P("CÁLCULO", 90, 490), P("I", 122, 490),
            P("10", 300, 490), P("95", 340, 490), P("2024-2", 400, 490),
            P("2", 22, 480), P("04130", 50, 480), P("FÍSICA", 90, 480), P("I", 115, 480),
            P("8", 302, 480), P("NA", 340, 480), P("2025-1", 400, 480),
        };

        var materias = KardexPdfParser.Leer(palabras);

        Assert.Equal(2, materias.Count);
        Assert.Equal(("CÁLCULO I", (short?)1, "2024-2", 95m), (materias[0].Materia, materias[0].Semestre, materias[0].Periodo, materias[0].Calificacion));
        Assert.Equal(("FÍSICA I", (short?)2, EstadoMateria.Reprobada, (short)8), (materias[1].Materia, materias[1].Semestre, materias[1].Estado, materias[1].Creditos));
    }

    [Fact]
    public void ReturnsNothingWithoutTableHeader()
    {
        var palabras = new[] { new PalabraPdf("Hola", 10, 20, 100, 1), new PalabraPdf("95", 40, 10, 100, 1) };
        Assert.Empty(KardexPdfParser.Leer(palabras));
    }

    private static KardexController AsUser(MiUniDbContext db, Guid user) => new(db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"))
            }
        }
    };

    [Fact]
    public async Task ImportPreviewsWithoutSavingAndReplaceSavesConfirmedList()
    {
        await using var db = new MiUniDbContext(new DbContextOptionsBuilder<MiUniDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        db.KardexMaterias.Add(new KardexMateria { Id = Guid.NewGuid(), UsuarioId = user, Materia = "VIEJA", Estado = EstadoMateria.Cursando });
        await db.SaveChangesAsync();
        var controller = AsUser(db, user);

        var texto = new MemoryStream(Encoding.UTF8.GetBytes("no soy un pdf"));
        Assert.IsType<BadRequestObjectResult>(await controller.Importar(
            new FormFile(texto, 0, texto.Length, "archivo", "kardex.pdf"), default));

        await using var pdf = File.OpenRead(Fixture);
        var ok = Assert.IsType<OkObjectResult>(await controller.Importar(
            new FormFile(pdf, 0, pdf.Length, "archivo", "kardex.pdf"), default));
        var vista = Assert.IsAssignableFrom<IEnumerable<KardexMateriaImportadaDto>>(ok.Value).ToList();
        Assert.Equal(6, vista.Count);
        Assert.Equal("VIEJA", (await db.KardexMaterias.SingleAsync()).Materia);

        var guardado = Assert.IsType<OkObjectResult>(await controller.Reemplazar(vista.Select(m => new GuardarKardexMateriaRequest
        {
            Materia = m.Materia, Clave = m.Clave, Periodo = m.Periodo, Semestre = m.Semestre,
            Calificacion = m.Calificacion, Creditos = m.Creditos, Estado = m.Estado
        }).ToList(), default));
        var kardex = Assert.IsType<KardexDto>(guardado.Value);
        Assert.Equal(6, kardex.Materias.Count);
        Assert.Equal(22, kardex.Resumen.CreditosAprobados);
        Assert.DoesNotContain(db.KardexMaterias, m => m.Materia == "VIEJA");
    }
}
