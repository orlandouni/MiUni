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

    private static ChatController AsUser(ChatController controller, Guid user)
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
        public Task<string> GetAnswerAsync(string question, CancellationToken ct) => Task.FromResult(answer);

        public async IAsyncEnumerable<string> StreamAnswerAsync(
            string question,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            yield return answer;
        }
    }
}
