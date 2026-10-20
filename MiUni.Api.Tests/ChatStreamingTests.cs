using System.Runtime.CompilerServices;
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

public class ChatStreamingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailureOrEmptyStreamCanReturnJson502BeforeStartingText(bool fail)
    {
        await using var db = Database();
        var controller = await Controller(db, new StreamRag(fail, empty: true));
        var result = Assert.IsType<ObjectResult>(await controller.Stream(
            new ChatRequest { Mensaje = "Pregunta de prueba" }, default));
        Assert.Equal(502, result.StatusCode);
        // Setting text/plain before this result prevents JSON negotiation and produces 406.
        Assert.Null(controller.Response.ContentType);
        Assert.Single(await db.Historialchats.ToListAsync());
    }

    [Fact]
    public async Task StreamReturnsSessionHeaderAndPersistsExactlyTheDeliveredText()
    {
        await using var db = Database();
        var controller = await Controller(db, new StreamRag(false, empty: false));
        await controller.Stream(new ChatRequest { Mensaje = "Pregunta de prueba" }, default);
        controller.Response.Body.Position = 0;
        var delivered = await new StreamReader(controller.Response.Body).ReadToEndAsync();
        Assert.Equal("Respuesta en partes.", delivered);
        Assert.Equal("text/plain; charset=utf-8", controller.Response.ContentType);
        Assert.True(Guid.TryParse(controller.Response.Headers["X-Session-Id"], out var session));
        var saved = await db.Historialchats.SingleAsync(m => m.Rol == "assistant");
        Assert.Equal(delivered, saved.Contenido);
        Assert.Equal(session, saved.SesionId);
    }

    private static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<ChatController> Controller(MiUniDbContext db, IRagClient rag)
    {
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Prueba streaming" });
        await db.SaveChangesAsync();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"));
        http.Response.Body = new MemoryStream();
        return new ChatController(db, rag) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    private sealed class StreamRag(bool fail, bool empty) : IRagClient
    {
        public Task<string> GetAnswerAsync(string q, string? auth, CancellationToken ct) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<string> StreamAnswerAsync(
            string q, string? auth, [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            if (fail) throw new HttpRequestException("Upstream unavailable");
            if (empty) yield break;
            yield return "Respuesta ";
            yield return "en partes.";
        }
    }
}
