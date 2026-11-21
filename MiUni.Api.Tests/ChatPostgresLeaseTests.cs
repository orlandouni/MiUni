using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public sealed class ChatPostgresFactAttribute : FactAttribute
{
    public ChatPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MIUNI_CHAT_TEST_DB")))
            Skip = "Set MIUNI_CHAT_TEST_DB to a local PostgreSQL test connection; no stored rows are changed.";
    }
}

public class ChatPostgresLeaseTests
{
    [ChatPostgresFact]
    public async Task SeparateConnectionsConflictOnlyForSameOwnedSessionAndRelease()
    {
        var options = new DbContextOptionsBuilder<MiUniDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("MIUNI_CHAT_TEST_DB"), x => x.UseNetTopologySuite()).Options;
        await using var a = new MiUniDbContext(options);
        await using var b = new MiUniDbContext(options);
        var user = Guid.NewGuid(); var session = Guid.NewGuid();
        var first = await ChatTurnLease.TryAcquire(a, user, session, default);
        Assert.NotNull(first);
        try
        {
            Assert.Null(await ChatTurnLease.TryAcquire(b, user, session, default));
            await using var otherSession = await ChatTurnLease.TryAcquire(b, user, Guid.NewGuid(), default);
            Assert.NotNull(otherSession);
        }
        finally { await first.DisposeAsync(); }
        await using var reacquired = await ChatTurnLease.TryAcquire(b, user, session, default);
        Assert.NotNull(reacquired);
    }
}
