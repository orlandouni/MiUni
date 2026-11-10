using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Controllers;
using MiUni.Api.DTOs;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class HorarioTests
{
    private static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HorarioController AsUser(MiUniDbContext db, Guid user) => new(db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"))
            }
        }
    };

    private static async Task<Guid> SeedUser(MiUniDbContext db)
    {
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        await db.SaveChangesAsync();
        return user;
    }

    private static GuardarHorarioClaseRequest Clase(string materia, string? profesor, short dia, int hora) => new()
    {
        Materia = materia, Profesor = profesor, Dia = dia,
        HoraInicio = new TimeOnly(hora, 0), HoraFin = new TimeOnly(hora + 1, 0),
    };

    [Fact]
    public async Task CrudKeepsClassesPrivateAndAddsBuhoRaterLink()
    {
        await using var db = Database();
        var user = await SeedUser(db);
        var other = await SeedUser(db);
        var controller = AsUser(db, user);

        var created = Assert.IsType<CreatedAtActionResult>(await controller.Create(
            Clase("  Cálculo   I ", "  Juan   Pérez ", 1, 9), default));
        var dto = Assert.IsType<HorarioClaseDto>(created.Value);
        Assert.Equal("Cálculo I", dto.Materia);
        Assert.Equal("Juan Pérez", dto.Profesor);
        Assert.Equal(BuhoRater.UrlBusqueda("Juan Pérez"), dto.BuhoRaterUrl);

        Assert.IsType<NotFoundResult>(await AsUser(db, other).GetById(dto.Id, default));
        Assert.IsType<NotFoundResult>(await AsUser(db, other).Delete(dto.Id, default));

        Assert.IsType<NoContentResult>(await controller.Update(dto.Id, Clase("Cálculo I", "", 2, 10), default));
        var updated = await db.HorarioClases.SingleAsync();
        Assert.Null(updated.Profesor);
        Assert.Equal((short)2, updated.Dia);

        Assert.IsType<NoContentResult>(await controller.Delete(dto.Id, default));
        Assert.Empty(db.HorarioClases);
    }

    [Fact]
    public async Task RejectsEndBeforeStart()
    {
        await using var db = Database();
        var user = await SeedUser(db);
        var request = Clase("Física", null, 3, 12);
        request.HoraFin = request.HoraInicio;
        Assert.IsType<BadRequestObjectResult>(await AsUser(db, user).Create(request, default));
        Assert.Empty(db.HorarioClases);
    }

    [Fact]
    public async Task ListsClassesInOrderAndGroupsProfessors()
    {
        await using var db = Database();
        var user = await SeedUser(db);
        var controller = AsUser(db, user);
        await controller.Create(Clase("Física", "Ana López", 3, 12), default);
        await controller.Create(Clase("Cálculo", "Juan Pérez", 1, 9), default);
        await controller.Create(Clase("Cálculo", "juan pérez", 3, 9), default);
        await controller.Create(Clase("Tutoría", null, 2, 8), default);

        var clases = Assert.IsAssignableFrom<IEnumerable<HorarioClaseDto>>(
            Assert.IsType<OkObjectResult>(await controller.GetAll(default)).Value).ToList();
        Assert.Equal(["Cálculo", "Tutoría", "Cálculo", "Física"], clases.Select(c => c.Materia));
        Assert.Null(clases[1].BuhoRaterUrl);

        var profesores = Assert.IsAssignableFrom<System.Collections.IEnumerable>(
            Assert.IsType<OkObjectResult>(await controller.GetProfesores(default)).Value).Cast<object>().ToList();
        Assert.Equal(2, profesores.Count);
    }

    [Fact]
    public void BuhoRaterLinkSearchesOnlyThatSite()
    {
        Assert.Null(BuhoRater.UrlBusqueda("   "));
        var url = BuhoRater.UrlBusqueda("María  José");
        Assert.StartsWith("https://www.google.com/search?q=", url);
        Assert.Contains(Uri.EscapeDataString("site:buhorater.com María José"), url);
    }
}
