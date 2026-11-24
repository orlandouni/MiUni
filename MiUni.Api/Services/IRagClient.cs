namespace MiUni.Api.Services;

public interface IRagClient
{
    Task<string> GetAnswerAsync(RagChatInput input, string? authorizationHeader, CancellationToken ct) =>
        GetAnswerAsync(input.Question, authorizationHeader, ct);

    IAsyncEnumerable<string> StreamAnswerAsync(RagChatInput input, string? authorizationHeader, CancellationToken ct) =>
        StreamAnswerAsync(input.Question, authorizationHeader, ct);

    Task<string> GetAnswerAsync(string question, string? authorizationHeader, CancellationToken ct);

    IAsyncEnumerable<string> StreamAnswerAsync(string question, string? authorizationHeader, CancellationToken ct);
}
