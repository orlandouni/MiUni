using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiUni.Api.Controllers;
using MiUni.Api.DTOs;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class HorarioPdfParserTests
{
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "horario-ejemplo.pdf");

    // Fixtures/horario-ejemplo.pdf copia el acomodo del horario del portal de la UNISON
    // con datos inventados (ver generar_horario_ejemplo.py).
    [Fact]
    public void ReadsUnisonScheduleWithWrappedNamesAndCenteredHours()
    {
        using var pdf = File.OpenRead(Fixture);
        var clases = HorarioPdfParser.Leer(pdf);

        Assert.Equal(9, clases.Count);
        var poo = clases.Where(c => c.Materia == "PROGRAMACIÓN ORIENTADA A OBJETOS II").ToList();
        Assert.Equal([1, 2, 3, 4], poo.Select(c => (int)c.Dia));
        Assert.All(poo, c =>
        {
            Assert.Equal("PÉREZ GARCÍA JUAN CARLOS", c.Profesor);
            Assert.Equal(new TimeOnly(8, 0), c.HoraInicio);
            Assert.Equal(new TimeOnly(9, 0), c.HoraFin);
            Assert.Equal("5G-A303", c.Salon);
        });

        var bd = clases.Where(c => c.Materia == "BASES DE DATOS").ToList();
        Assert.Equal([2, 4], bd.Select(c => (int)c.Dia));
        Assert.All(bd, c => Assert.Equal(new TimeOnly(12, 0), c.HoraFin));

        var propiedad = clases.Where(c => c.Materia == "PROPIEDAD INTELECTUAL Y EJERCICIO PROFESIONAL").ToList();
        Assert.Equal([1, 3], propiedad.Select(c => (int)c.Dia));
        Assert.All(propiedad, c => Assert.Equal("MORALES SOTO LUCÍA", c.Profesor));

        var cultura = Assert.Single(clases, c => c.Materia == "CULTURA EMPRENDEDORA");
        Assert.Null(cultura.Profesor);
        Assert.Equal((short)5, cultura.Dia);
        Assert.Equal(new TimeOnly(7, 0), cultura.HoraInicio);
    }

    [Fact]
    public void ReturnsNothingWithoutDayHeader()
    {
        var palabras = new[] { new PalabraPdf("Hola", 10, 20, 100, 1), new PalabraPdf("7:00-8:00", 40, 30, 100, 1) };
        Assert.Empty(HorarioPdfParser.Leer(palabras));
    }

    [Fact]
    public async Task ImportEndpointRejectsNonPdfAndReturnsPreviewForPdf()
    {
        var controller = new HorarioController(null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"))
            }
        };

        var texto = new MemoryStream(Encoding.UTF8.GetBytes("no soy un pdf"));
        Assert.IsType<BadRequestObjectResult>(await controller.Importar(
            new FormFile(texto, 0, texto.Length, "archivo", "horario.pdf"), default));

        await using var pdf = File.OpenRead(Fixture);
        var ok = Assert.IsType<OkObjectResult>(await controller.Importar(
            new FormFile(pdf, 0, pdf.Length, "archivo", "horario.pdf"), default));
        var clases = Assert.IsAssignableFrom<IEnumerable<ClaseImportadaDto>>(ok.Value).ToList();
        Assert.Equal(9, clases.Count);
        Assert.Contains(clases, c => c.BuhoRaterUrl is not null && c.Profesor == "LÓPEZ RUIZ ANA");
    }
}
