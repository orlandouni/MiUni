using System.Text;
using System.Text.Json.Serialization;
using MiUni.Api.Models;

namespace MiUni.Api.Services;

public sealed record RagHistoryMessage(string Role, string Content);
public sealed record RagScheduleRow(string Subject, int Day, string Start, string End, string? Room, string? Teacher);
public sealed record RagPersonalContext(IReadOnlyList<RagScheduleRow> Schedule,
    [property: JsonPropertyName("campus_now")] string CampusNow);
public sealed record RagChatInput(string Question, IReadOnlyList<RagHistoryMessage> History,
    [property: JsonPropertyName("personal_context")] RagPersonalContext? PersonalContext = null);

public static class ChatContext
{
    public const int MaxMessages = 12;
    public const int MaxHistoryBytes = 4000;

    // A UTF-8 byte budget conservatively bounds byte-tokenizer input without a new tokenizer dependency.
    public static IReadOnlyList<RagHistoryMessage> CompletedTurns(IEnumerable<Historialchat> rows,
        int maxMessages = MaxMessages, int maxBytes = MaxHistoryBytes)
    {
        maxMessages = Math.Clamp(maxMessages, 2, MaxMessages);
        maxBytes = Math.Clamp(maxBytes, 128, MaxHistoryBytes);
        var pairs = new List<(RagHistoryMessage User, RagHistoryMessage Assistant)>();
        Historialchat? pending = null;
        foreach (var row in rows)
        {
            if (row.Rol == "user") pending = row;
            else if (row.Rol == "assistant" && pending is not null)
            {
                pairs.Add((new("user", pending.Contenido), new("assistant", row.Contenido)));
                pending = null;
            }
            else pending = null;
        }
        var selected = new List<RagHistoryMessage>();
        var bytes = 0;
        foreach (var pair in pairs.AsEnumerable().Reverse())
        {
            var size = Encoding.UTF8.GetByteCount(pair.User.Content) + Encoding.UTF8.GetByteCount(pair.Assistant.Content);
            if (selected.Count + 2 > maxMessages || bytes + size > maxBytes) break;
            selected.InsertRange(0, [pair.User, pair.Assistant]);
            bytes += size;
        }
        return selected;
    }
}
