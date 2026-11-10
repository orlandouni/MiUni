using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Controllers;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class BuhoRaterImportTests
{
    private const string Soto = "0136eb39-3a7c-4b75-af52-92ff2283d434";
    private const string Lopez = "1a402933-46b8-443d-82ac-2f6f69305e07";

    [Fact]
    public void ReadsProfileLinksAndNamesFromSavedPage()
    {
        var html = $$"""
            <html><head><script>var x = '<a href="/perfil?id={{Lopez}}">NO</a>';</script></head><body>
            <div class="card"><a href="https://www.buhorater.com/perfil?id={{Soto}}"><h3>SOTO MORA CLAUDIA&nbsp;PAULINA</h3></a></div>
            <div class="card"><a href='/perfil?id={{Lopez}}' title="López Ruiz Ana">Ver perfil</a></div>
            <a href="/perfil?id={{Soto}}">Ver perfil</a>
            <a href="https://otro.com/perfil?id={{Soto}}">Falso</a>
            <a href="/perfil?id=2c9e1f55-0000-4000-8000-000000000000">Ver perfil</a>
            </body></html>
            """;
        var resultado = BuhoRaterImport.LeerHtml(html);
        Assert.Equal(2, resultado.Perfiles.Count);
        Assert.Contains(new PerfilImportado("SOTO MORA CLAUDIA PAULINA", Guid.Parse(Soto)), resultado.Perfiles);
        Assert.Contains(new PerfilImportado("López Ruiz Ana", Guid.Parse(Lopez)), resultado.Perfiles);
        Assert.Equal(1, resultado.SinNombre);
    }

    [Fact]
    public void ReadsCsvInEitherColumnOrder()
    {
        var csv = $"nombre,enlace\nSOTO MORA CLAUDIA PAULINA,https://www.buhorater.com/perfil?id={Soto}\n{Lopez};\"López Ruiz Ana\"\n";
        var resultado = BuhoRaterImport.Leer(csv, "profes.csv");
        Assert.Equal(["SOTO MORA CLAUDIA PAULINA", "López Ruiz Ana"], resultado.Perfiles.Select(p => p.Nombre));
    }

    [Fact]
    public async Task ImportUpsertsProfilesSoScheduleLinksGoStraightToThem()
    {
        await using var db = new MiUniDbContext(new DbContextOptionsBuilder<MiUniDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var admin = Guid.NewGuid();
        db.Users.Add(new MiUni.Api.Identity.ApplicationUser { Id = admin, Nombre = "Admin" });
        db.ProfesorBuhoRaters.Add(new ProfesorBuhoRater { Clave = "LOPEZ RUIZ ANA", Nombre = "LÓPEZ RUIZ ANA", PerfilId = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var controller = new HorarioController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, admin.ToString())], "test"))
                }
            }
        };

        var html = Encoding.UTF8.GetBytes($"<a href=\"/perfil?id={Soto}\">SOTO MORA CLAUDIA PAULINA</a><a href=\"/perfil?id={Lopez}\">LÓPEZ RUIZ ANA</a>");
        var ok = Assert.IsType<OkObjectResult>(await controller.ImportarBuhoRater(
            new FormFile(new MemoryStream(html), 0, html.Length, "archivo", "buhorater.html"), default));
        Assert.Contains("nuevos = 1", ok.Value!.ToString());
        Assert.Contains("actualizados = 1", ok.Value!.ToString());
        Assert.Equal(Guid.Parse(Lopez), (await db.ProfesorBuhoRaters.SingleAsync(p => p.Clave == "LOPEZ RUIZ ANA")).PerfilId);

        var vacio = Encoding.UTF8.GetBytes("<p>nada</p>");
        Assert.IsType<UnprocessableEntityObjectResult>(await controller.ImportarBuhoRater(
            new FormFile(new MemoryStream(vacio), 0, vacio.Length, "archivo", "x.html"), default));
    }

    [Fact]
    public void ImportIsAdminOnly()
    {
        var metodo = typeof(HorarioController).GetMethod(nameof(HorarioController.ImportarBuhoRater))!;
        var autorizacion = Assert.Single(metodo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
        Assert.Equal(MiUni.Api.Identity.AppRoles.Administrador, autorizacion.Roles);
    }
}
