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

public class ChatContextTests
{
    internal static MiUniDbContext Database() => new(new DbContextOptionsBuilder<MiUniDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    internal static ChatController Controller(MiUniDbContext db, IRagClient rag, Guid user) => new(db, rag)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test"))
        }}
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryQuestionStaysRawWithoutAcademicProfile(bool stream)
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Private name" });
        await db.SaveChangesAsync();
        var rag = new CaptureRag();
        var controller = Controller(db, rag, user);
        controller.Response.Body = new MemoryStream();
        var request = new ChatRequest { Mensaje = "Que intercambios estan disponibles?" };
        if (stream) await controller.Stream(request, default);
        else await controller.Send(request, default);
        Assert.Equal(request.Mensaje, rag.Question);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedScheduleAnswersWithoutRag(bool stream)
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Student" });
        db.HorarioClases.Add(new HorarioClase { Id = Guid.NewGuid(), UsuarioId = user,
            Materia = "Databases", Dia = 2, HoraInicio = new(9, 0), HoraFin = new(10, 0) });
        await db.SaveChangesAsync();
        var rag = new CaptureRag();
        var controller = Controller(db, rag, user);
        controller.Response.Body = new MemoryStream();
        var request = new ChatRequest { Mensaje = "Que clase tengo el martes?" };
        if (stream) await controller.Stream(request, default);
        else await controller.Send(request, default);
        Assert.Equal(0, rag.Calls);
        var answer = await db.Historialchats.SingleAsync(m => m.Rol == "assistant");
        Assert.Contains("Databases", answer.Contenido);
        Assert.Contains("/horario", answer.Contenido);
    }

    internal class CaptureRag : IRagClient
    {
        public int Calls;
        public string? Question;
        public RagChatInput? Input;
        public Task<string> GetAnswerAsync(RagChatInput input, string? auth, CancellationToken ct)
        { Input = input; return GetAnswerAsync(input.Question, auth, ct); }
        public IAsyncEnumerable<string> StreamAnswerAsync(RagChatInput input, string? auth, CancellationToken ct)
        { Input = input; return StreamAnswerAsync(input.Question, auth, ct); }
        public Task<string> GetAnswerAsync(string q, string? auth, CancellationToken ct)
        { Calls++; Question = q; return Task.FromResult("Answer"); }
        public async IAsyncEnumerable<string> StreamAnswerAsync(string q, string? auth,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        { Calls++; Question = q; await Task.Yield(); yield return "Answer"; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReloadUsesOnlyCompletedOwnedTurnsAndKeepsDuplicateCurrentText(bool stream)
    {
        await using var db = Database();
        var user = Guid.NewGuid(); var other = Guid.NewGuid(); var session = Guid.NewGuid();
        db.Users.AddRange(new ApplicationUser { Id = user, Nombre = "Owner" }, new ApplicationUser { Id = other, Nombre = "Other" });
        var time = DateTime.UtcNow.AddMinutes(-10);
        void Row(Guid owner, Guid chat, string role, string text, int minute) => db.Historialchats.Add(new Historialchat
        { Id = Guid.NewGuid(), UsuarioId = owner, SesionId = chat, Rol = role, Contenido = text, FechaCreacion = time.AddMinutes(minute) });
        Row(user, session, "user", "Academic exchanges", 0);
        Row(user, session, "assistant", "Verified exchange information", 1);
        Row(user, session, "user", "What documents?", 2); // Failed turn: no assistant.
        Row(user, session, "user", "What documents?", 3);
        Row(user, session, "assistant", "Confirmed documents", 4);
        Row(other, session, "user", "FOREIGN", 5);
        Row(other, session, "assistant", "FOREIGN", 6);
        Row(user, Guid.NewGuid(), "user", "OTHER CHAT", 0);
        Row(user, Guid.NewGuid(), "assistant", "OTHER CHAT", 1);
        await db.SaveChangesAsync();
        var rag = new CaptureRag(); var controller = Controller(db, rag, user);
        controller.Response.Body = new MemoryStream();
        var request = new ChatRequest { Mensaje = "What documents?", SesionId = session };
        if (stream) await controller.Stream(request, default); else await controller.Send(request, default);
        Assert.NotNull(rag.Input);
        Assert.Equal(["Academic exchanges", "Verified exchange information", "What documents?", "Confirmed documents"],
            rag.Input.History.Select(m => m.Content));
        Assert.Equal("What documents?", rag.Input.Question);
        Assert.Null(rag.Input.PersonalContext);
        Assert.Equal(3, await db.Historialchats.CountAsync(m => m.UsuarioId == user && m.Contenido == "What documents?"));
        var fresh = new CaptureRag();
        await Controller(db, fresh, user).Send(new ChatRequest { Mensaje = "What documents?" }, default);
        Assert.Empty(fresh.Input!.History);
        await controller.DeleteSession(session, default);
        Assert.IsType<NotFoundObjectResult>(await Controller(db, new CaptureRag(), user).Send(request, default));
    }

    [Fact]
    public void HistoryDropsOldestWholeTurnsAndCountsUtf8Bytes()
    {
        var rows = Enumerable.Range(0, 16).Select(i => new Historialchat
            { Rol = i % 2 == 0 ? "user" : "assistant", Contenido = new string('ñ', 200) }).ToList();
        var messages = ChatContext.CompletedTurns(rows);
        Assert.Equal(10, messages.Count); // 400 bytes each; 4,000 byte budget.
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("assistant", messages[^1].Role);
        Assert.Empty(ChatContext.CompletedTurns([new() { Rol = "user", Contenido = "orphan" }]));
        Assert.Equal(12, ChatContext.CompletedTurns(rows.Select(r => new Historialchat { Rol = r.Rol, Contenido = "x" })).Count);
    }

    [Fact]
    public async Task OverlappingTurnAndDeleteConflictAndGuardReleasesAfterFailure()
    {
        await using var db = Database(); var user = Guid.NewGuid(); var session = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Owner" });
        db.Historialchats.Add(new Historialchat { Id = Guid.NewGuid(), UsuarioId = user, SesionId = session,
            Rol = "user", Contenido = "old orphan", FechaCreacion = DateTime.UtcNow.AddHours(-1) });
        await db.SaveChangesAsync();
        var rag = new BlockingRag(); var request = new ChatRequest { Mensaje = "About exchanges", SesionId = session };
        var pending = Controller(db, rag, user).Send(request, default);
        await rag.Started.Task;
        Assert.IsType<ConflictObjectResult>(await Controller(db, new CaptureRag(), user).Send(request, default));
        Assert.IsType<ConflictObjectResult>(await Controller(db, new CaptureRag(), user).DeleteSession(session, default));
        rag.Release.SetResult(); await pending;
        var next = new CaptureRag();
        Assert.IsType<OkObjectResult>(await Controller(db, next, user).Send(request, default));
        Assert.Empty(next.Input!.History); // Failed generation never becomes a completed turn.
    }

    private sealed class BlockingRag : IRagClient
    {
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> GetAnswerAsync(string q, string? a, CancellationToken ct)
        { Started.SetResult(); await Release.Task; throw new HttpRequestException("Failed provider"); }
        public IAsyncEnumerable<string> StreamAnswerAsync(string q, string? a, CancellationToken ct) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedQuestionWithoutScheduleStillQueriesInstitutionalSources(bool stream)
    {
        await using var db = Database(); var user = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = user, Nombre = "Owner" }); await db.SaveChangesAsync();
        var rag = new CaptureRag(); var controller = Controller(db, rag, user);
        controller.Response.Body = new MemoryStream();
        var request = new ChatRequest { Mensaje = "Mi horario y requisitos para becas" };
        if (stream) await controller.Stream(request, default); else await controller.Send(request, default);
        Assert.Equal(1, rag.Calls);
        Assert.Equal(request.Mensaje, rag.Input!.Question);
        Assert.Empty(rag.Input.PersonalContext!.Schedule);
    }

    [Fact]
    public void ConfiguredHistoryBudgetStillPreservesWholeTurns()
    {
        var rows = Enumerable.Range(0, 8).Select(i => new Historialchat
            { Rol = i % 2 == 0 ? "user" : "assistant", Contenido = i.ToString() });
        Assert.Equal(["4", "5", "6", "7"], ChatContext.CompletedTurns(rows, 5, 4000).Select(m => m.Content));
    }
}
