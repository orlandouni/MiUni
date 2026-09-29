namespace MiUni.Api.Services;

public interface IRagClient
{
    Task<string> GetAnswerAsync(string question, CancellationToken ct);

    IAsyncEnumerable<string> StreamAnswerAsync(string question, CancellationToken ct);
}
