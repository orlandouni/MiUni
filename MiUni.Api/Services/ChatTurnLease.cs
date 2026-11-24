using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MiUni.Api.Models;

namespace MiUni.Api.Services;

/// <summary>Nonblocking turn/delete serialization. PostgreSQL locks also cover other API instances.</summary>
public sealed class ChatTurnLease : IAsyncDisposable
{
    private static readonly ConcurrentDictionary<string, byte> Local = new();
    private readonly string key;
    private readonly DbConnection? connection;
    private readonly bool closeConnection;
    private ChatTurnLease(string key, DbConnection? connection = null, bool closeConnection = false)
    { this.key = key; this.connection = connection; this.closeConnection = closeConnection; }

    public static async Task<ChatTurnLease?> TryAcquire(MiUniDbContext db, Guid user, Guid session, CancellationToken ct)
    {
        var key = $"miuni-chat:{user:N}:{session:N}";
        if (!db.Database.IsNpgsql())
            return Local.TryAdd(key, 0) ? new(key) : null;
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(ct);
        try
        {
            await using var command = Command(connection, "pg_try_advisory_lock", key);
            if (await command.ExecuteScalarAsync(ct) is true) return new(key, connection, opened);
            if (opened) await connection.CloseAsync();
            return null;
        }
        catch { if (opened) await connection.CloseAsync(); throw; }
    }

    private static DbCommand Command(DbConnection connection, string function, string key)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {function}(hashtextextended(@key, 0))";
        var parameter = command.CreateParameter(); parameter.ParameterName = "key"; parameter.Value = key;
        command.Parameters.Add(parameter);
        return command;
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is null) { Local.TryRemove(key, out _); return; }
        try
        {
            await using var command = Command(connection, "pg_advisory_unlock", key);
            await command.ExecuteScalarAsync(CancellationToken.None);
        }
        finally { if (closeConnection) await connection.CloseAsync(); }
    }
}
