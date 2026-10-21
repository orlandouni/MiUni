using System.Security.Claims;
using System.Runtime.CompilerServices;
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

public class ChatControllerTests
{
    private static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ChatController AsUser(ChatController controller, Guid user, string? authorizationHeader = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"))
        };

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task SendCreatesSessionAndStoresUserAndAssistantMessages()
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        await db.SaveChangesAsync();

        var controller = AsUser(new ChatController(db, new FakeRagClient("Respuesta")), user);

        var result = Assert.IsType<OkObjectResult>(await controller.Send(
            new ChatRequest { Mensaje = " Donde esta biblioteca? " }, default));
        var response = Assert.IsType<ChatResponse>(result.Value);

        Assert.Equal("Respuesta", response.Answer);
        Assert.Equal(2, response.Messages.Count);
        Assert.Equal(["user", "assistant"], response.Messages.Select(m => m.Rol).ToArray());
        Assert.All(response.Messages, message => Assert.Equal(response.SesionId, message.SesionId));
        Assert.Equal(["Donde esta biblioteca?", "Respuesta"],
            await db.Historialchats.OrderBy(h => h.FechaCreacion).Select(h => h.Contenido).ToListAsync());
    }

    [Fact]
    public async Task SendPassesAuthorizationHeaderToRagClient()
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        await db.SaveChangesAsync();

        var ragClient = new FakeRagClient("Respuesta");
        var controller = AsUser(
            new ChatController(db, ragClient),
            user,
            "Bearer token-de-prueba");

        await controller.Send(new ChatRequest { Mensaje = "Hola" }, default);

        Assert.Equal("Bearer token-de-prueba", ragClient.LastAuthorizationHeader);
    }

    [Fact]
    public async Task SendRejectsSessionFromAnotherUser()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var session = Guid.NewGuid();
        db.Users.AddRange(
            new ApplicationUser { Id = owner, Nombre = "Owner" },
            new ApplicationUser { Id = other, Nombre = "Other" });
        db.Historialchats.Add(new Historialchat
        {
            Id = Guid.NewGuid(),
            SesionId = session,
            UsuarioId = owner,
            Rol = "user",
            Contenido = "Hola",
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = AsUser(new ChatController(db, new FakeRagClient("Respuesta")), other);

        Assert.IsType<NotFoundObjectResult>(await controller.Send(
            new ChatRequest { SesionId = session, Mensaje = "Continuar" }, default));
        Assert.Single(db.Historialchats);
    }

    [Fact]
    public async Task GetSessionsGroupsOnlyAuthenticatedUsersMessages()
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var firstSession = Guid.NewGuid();
        var secondSession = Guid.NewGuid();
        var otherSession = Guid.NewGuid();
        db.Users.AddRange(
            new ApplicationUser { Id = user, Nombre = "Prueba" },
            new ApplicationUser { Id = other, Nombre = "Otro" });
        db.Historialchats.AddRange(
            Message(firstSession, user, "user", "Primera", DateTime.UtcNow.AddMinutes(-5)),
            Message(firstSession, user, "assistant", "Respuesta", DateTime.UtcNow.AddMinutes(-4)),
            Message(secondSession, user, "user", "Segunda", DateTime.UtcNow.AddMinutes(-1)),
            Message(otherSession, other, "user", "Privada", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await AsUser(new ChatController(db, new FakeRagClient("Respuesta")), user).GetSessions(default));
        var sessions = Assert.IsType<List<ChatSessionDto>>(result.Value);

        Assert.Equal(2, sessions.Count);
        Assert.Equal(secondSession, sessions[0].SesionId);
        Assert.DoesNotContain(sessions, s => s.SesionId == otherSession);
    }

    [Fact]
    public async Task GetSessionReturnsOnlyOwnersMessages()
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var session = Guid.NewGuid();
        db.Users.AddRange(
            new ApplicationUser { Id = user, Nombre = "Prueba" },
            new ApplicationUser { Id = other, Nombre = "Otro" });
        db.Historialchats.AddRange(
            Message(session, user, "user", "Hola", DateTime.UtcNow.AddMinutes(-1)),
            Message(session, user, "assistant", "Que tal", DateTime.UtcNow),
            Message(session, other, "user", "No visible", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await AsUser(new ChatController(db, new FakeRagClient("Respuesta")), user).GetSession(session, default));
        var messages = Assert.IsType<List<ChatMessageDto>>(result.Value);

        Assert.Equal(2, messages.Count);
        Assert.DoesNotContain(messages, m => m.Contenido == "No visible");
    }

    [Fact]
    public async Task DeleteSessionRemovesOnlyOwnersConversation()
    {
        await using var db = Database();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var session = Guid.NewGuid();
        db.Historialchats.AddRange(
            Message(session, owner, "user", "Hola", DateTime.UtcNow),
            Message(session, owner, "assistant", "Respuesta", DateTime.UtcNow),
            Message(session, other, "user", "Ajeno", DateTime.UtcNow),
            Message(Guid.NewGuid(), owner, "user", "Otra conversación", DateTime.UtcNow));
        await db.SaveChangesAsync();
        var controller = AsUser(new ChatController(db, new FakeRagClient("Respuesta")), owner);
        Assert.IsType<NoContentResult>(await controller.DeleteSession(session, default));
        Assert.Equal(2, await db.Historialchats.CountAsync());
        Assert.IsType<NotFoundResult>(await controller.DeleteSession(session, default));
        Assert.Equal(2, await db.Historialchats.CountAsync());
    }

    [Fact]
    public async Task DeleteSessionRequiresAuthentication()
    {
        await using var db = Database();
        var controller = new ChatController(db, new FakeRagClient("Respuesta"))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<UnauthorizedResult>(await controller.DeleteSession(Guid.NewGuid(), default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChatUsesCurrentActiveCatalogWithoutStoringItInHistory(bool streaming)
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba" });
        var category = new Categorium { Id = Guid.NewGuid(), Nombre = "Deportes" };
        db.Categoria.Add(category);
        foreach (var active in new[] { true, false })
            db.Lugars.Add(new Lugar
            {
                Id = Guid.NewGuid(), Nombre = active ? "Campo de beisbol" : "Lugar oculto",
                Categoria = category, CategoriaId = category.Id, Activo = active,
                Descripcion = "DESCRIPCION_PRIVADA_NO_ENVIAR",
                Ubicacion = new NetTopologySuite.Geometries.Point(-110.96, 29.08)
            });
        await db.SaveChangesAsync();
        var rag = new FakeRagClient("Campo de beisbol");
        var controller = AsUser(new ChatController(db, rag), user);
        controller.Response.Body = new MemoryStream();
        var request = new ChatRequest { Mensaje = "Donde jugar baseball?" };
        if (streaming) await controller.Stream(request, default);
        else await controller.Send(request, default);
        Assert.Contains("Campo de beisbol", rag.LastQuestion);
        Assert.Contains("Deportes", rag.LastQuestion);
        Assert.DoesNotContain("Lugar oculto", rag.LastQuestion);
        Assert.DoesNotContain("DESCRIPCION_PRIVADA_NO_ENVIAR", rag.LastQuestion);
        Assert.Contains(request.Mensaje, rag.LastQuestion);
        Assert.Equal(request.Mensaje, (await db.Historialchats.SingleAsync(h => h.Rol == "user")).Contenido);
    }

    private static Historialchat Message(Guid session, Guid user, string role, string content, DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            SesionId = session,
            UsuarioId = user,
            Rol = role,
            Contenido = content,
            FechaCreacion = createdAt
        };

    private class FakeRagClient(string answer) : IRagClient
    {
        public string? LastAuthorizationHeader { get; private set; }
        public string LastQuestion { get; private set; } = "";

        public Task<string> GetAnswerAsync(string question, string? authorizationHeader, CancellationToken ct)
        {
            LastAuthorizationHeader = authorizationHeader;
            LastQuestion = question;
            return Task.FromResult(answer);
        }

        public async IAsyncEnumerable<string> StreamAnswerAsync(
            string question,
            string? authorizationHeader,
            [EnumeratorCancellation] CancellationToken ct)
        {
            LastAuthorizationHeader = authorizationHeader;
            LastQuestion = question;
            await Task.Yield();
            yield return answer;
        }
    }
}
