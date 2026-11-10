using System.Security.Claims;
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

public class KardexTests
{
    private static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static KardexController AsUser(MiUniDbContext db, Guid user) => new(db)
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

    private static GuardarKardexMateriaRequest Materia(string nombre, EstadoMateria estado, decimal? calificacion,
        short creditos = 8, short semestre = 1) => new()
    {
        Materia = nombre, Estado = estado, Calificacion = calificacion, Creditos = creditos, Semestre = semestre
    };

    [Fact]
    public async Task CreaMateriasYCalculaResumen()
    {
        await using var db = Database();
        var user = await SeedUser(db);
        var controller = AsUser(db, user);

        await controller.Create(Materia("  Cálculo   I ", EstadoMateria.Aprobada, 90), default);
        await controller.Create(Materia("Física I", EstadoMateria.Reprobada, 50), default);
        await controller.Create(Materia("Álgebra", EstadoMateria.Aprobada, 80, creditos: 6), default);
        await controller.Create(Materia("Programación", EstadoMateria.Cursando, null, semestre: 2), default);

        var kardex = Assert.IsType<KardexDto>(Assert.IsType<OkObjectResult>(await controller.Get(default)).Value);
        Assert.Equal(4, kardex.Materias.Count);
        Assert.Contains(kardex.Materias, m => m.Materia == "Cálculo I");
        Assert.Equal(14, kardex.Resumen.CreditosAprobados);
        Assert.Equal(73.33m, kardex.Resumen.Promedio);
        Assert.Equal(2, kardex.Resumen.MateriasAprobadas);
        Assert.Equal(1, kardex.Resumen.MateriasReprobadas);
        Assert.Equal(1, kardex.Resumen.MateriasCursando);
    }

    [Fact]
    public async Task ExigeCalificacionEnMateriasTerminadas()
    {
        await using var db = Database();
        var user = await SeedUser(db);

        var result = await AsUser(db, user).Create(Materia("Cálculo I", EstadoMateria.Aprobada, null), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.KardexMaterias);
    }

    [Fact]
    public async Task NoPermiteVerEditarNiBorrarMateriasDeOtroUsuario()
    {
        await using var db = Database();
        var owner = await SeedUser(db);
        var other = await SeedUser(db);
        var created = Assert.IsType<CreatedAtActionResult>(
            await AsUser(db, owner).Create(Materia("Cálculo I", EstadoMateria.Aprobada, 90), default));
        var id = Assert.IsType<KardexMateriaDto>(created.Value).Id;
        var intruso = AsUser(db, other);

        Assert.IsType<NotFoundResult>(await intruso.GetById(id, default));
        Assert.IsType<NotFoundResult>(await intruso.Update(id, Materia("X", EstadoMateria.Baja, null), default));
        Assert.IsType<NotFoundResult>(await intruso.Delete(id, default));
        var kardex = Assert.IsType<KardexDto>(Assert.IsType<OkObjectResult>(await intruso.Get(default)).Value);
        Assert.Empty(kardex.Materias);
        Assert.Equal("Cálculo I", (await db.KardexMaterias.SingleAsync()).Materia);
    }

    [Fact]
    public async Task ActualizaYBorraMateriaPropia()
    {
        await using var db = Database();
        var user = await SeedUser(db);
        var controller = AsUser(db, user);
        var created = Assert.IsType<CreatedAtActionResult>(
            await controller.Create(Materia("Programación", EstadoMateria.Cursando, null), default));
        var id = Assert.IsType<KardexMateriaDto>(created.Value).Id;

        Assert.IsType<NoContentResult>(await controller.Update(id, Materia("Programación", EstadoMateria.Aprobada, 95), default));
        var materia = await db.KardexMaterias.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoMateria.Aprobada, materia.Estado);
        Assert.Equal(95, materia.Calificacion);

        Assert.IsType<NoContentResult>(await controller.Delete(id, default));
        Assert.Empty(db.KardexMaterias);
    }

    [Fact]
    public void FechaHoraCampusUsaHoraDeHermosillo()
    {
        // 2026-11-10 15:30 UTC es martes 08:30 en Hermosillo (UTC-7).
        Assert.Equal("martes 2026-11-10 08:30 (hora de Hermosillo)",
            PerfilAcademico.FechaHoraCampus(new DateTime(2026, 11, 10, 15, 30, 0, DateTimeKind.Utc)));
    }
}
