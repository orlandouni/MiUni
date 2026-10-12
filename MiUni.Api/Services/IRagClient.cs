namespace MiUni.Api.Services;

public interface IRagClient
{
    Task<string> GetAnswerAsync(string question, string? authorizationHeader, CancellationToken ct);

    IAsyncEnumerable<string> StreamAnswerAsync(string question, string? authorizationHeader, CancellationToken ct);
}
