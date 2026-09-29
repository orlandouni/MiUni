using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Controllers;
using MiUni.Api.DTOs;
using MiUni.Api.Identity;
using MiUni.Api.Models;
using NetTopologySuite.Geometries;
using Xunit;

namespace MiUni.Api.Tests;

public class ResenaFavoritoTests
{
    private static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static T AsUser<T>(T controller, Guid user) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"))
            }
        };
        return controller;
    }

    private static async Task<(Guid User, Guid Place)> Seed(MiUniDbContext db)
    {
        var user = Guid.NewGuid();
        var place = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        db.Lugars.Add(new Lugar { Id = place, Nombre = "Biblioteca", Activo = true, Ubicacion = new Point(0, 0) });
        await db.SaveChangesAsync();
        return (user, place);
    }

    [Fact]
    public async Task ResenaCrudPreservesAuthorAndCreationDate()
    {
        await using var db = Database();
        var (user, place) = await Seed(db);
        var controller = AsUser(new ResenaController(db), user);
        var created = Assert.IsType<CreatedAtActionResult>(await controller.Create(
            new CrearResenaRequest { LugarId = place, Calificacion = 4, Comentario = " Bien " }, default));
        var dto = Assert.IsType<ResenaDto>(created.Value);
        Assert.Equal(user, dto.UsuarioId);
        Assert.Equal("Bien", dto.Comentario);
        Assert.IsType<OkObjectResult>(await controller.GetById(dto.Id, default));
        Assert.IsType<NoContentResult>(await controller.Update(dto.Id,
            new ActualizarResenaRequest { Calificacion = 5, Comentario = "Mejor" }, default));
        Assert.Equal(dto.FechaCreacion, (await db.Resenas.SingleAsync()).FechaCreacion);
        Assert.Equal((short)5, (await db.Resenas.SingleAsync()).Calificacion);
        Assert.IsType<NoContentResult>(await controller.Delete(dto.Id, default));
        Assert.IsType<NotFoundResult>(await controller.GetById(dto.Id, default));
    }

    [Fact]
    public async Task OtherUsersCannotChangeResenasAndReportsPreventDeletion()
    {
        await using var db = Database();
        var (user, place) = await Seed(db);
        var resena = new Resena { Id = Guid.NewGuid(), UsuarioId = user, LugarId = place, Calificacion = 3 };
        db.Resenas.Add(resena);
        await db.SaveChangesAsync();
        var other = AsUser(new ResenaController(db), Guid.NewGuid());
        Assert.IsType<ForbidResult>(await other.Update(resena.Id, new ActualizarResenaRequest { Calificacion = 1 }, default));
        Assert.IsType<ForbidResult>(await other.Delete(resena.Id, default));
        db.Reporteusuarios.Add(new Reporteusuario { Id = Guid.NewGuid(), UsuarioId = user, ResenaId = resena.Id, Motivo = "Prueba" });
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await AsUser(new ResenaController(db), user).Delete(resena.Id, default));
        Assert.Single(db.Resenas);
    }

    [Fact]
    public async Task FavoritesArePrivateAndPutIsIdempotent()
    {
        await using var db = Database();
        var (user, place) = await Seed(db);
        var controller = AsUser(new FavoritoController(db), user);
        Assert.IsType<CreatedAtActionResult>(await controller.Create(new CrearFavoritoRequest { LugarId = place }, default));
        Assert.IsType<ConflictObjectResult>(await controller.Create(new CrearFavoritoRequest { LugarId = place }, default));
        var date = (await db.Favoritos.SingleAsync()).FechaAgregado;
        Assert.IsType<NoContentResult>(await controller.Put(place, default));
        Assert.Equal(date, (await db.Favoritos.SingleAsync()).FechaAgregado);
        var other = AsUser(new FavoritoController(db), Guid.NewGuid());
        Assert.IsType<NotFoundResult>(await other.GetById(place, default));
        Assert.IsType<NotFoundResult>(await other.Delete(place, default));
        var list = Assert.IsType<OkObjectResult>(await other.GetAll(default));
        Assert.Empty(Assert.IsType<List<FavoritoDto>>(list.Value));
        Assert.IsType<NoContentResult>(await controller.Delete(place, default));
        Assert.IsType<CreatedAtActionResult>(await controller.Put(place, default));
    }

    [Fact]
    public async Task MissingOrInactivePlacesAreRejected()
    {
        await using var db = Database();
        var (user, place) = await Seed(db);
        (await db.Lugars.SingleAsync()).Activo = false;
        await db.SaveChangesAsync();
        foreach (var id in new[] { place, Guid.Empty })
        {
            Assert.IsType<BadRequestObjectResult>(await AsUser(new FavoritoController(db), user).Put(id, default));
            Assert.IsType<BadRequestObjectResult>(await AsUser(new ResenaController(db), user).Create(
                new CrearResenaRequest { LugarId = id, Calificacion = 4 }, default));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void InvalidRatingsFailModelValidation(short rating)
    {
        var request = new ActualizarResenaRequest { Calificacion = rating };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
