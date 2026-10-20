using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace MiUni.Api.Services;

public class RagClient(HttpClient httpClient) : IRagClient
{
    public async Task<string> GetAnswerAsync(string question, string? authorizationHeader, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, "chat", question, authorizationHeader);

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RagQueryResponse>(cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(body?.Answer))
        {
            throw new InvalidOperationException("El RAG no devolvio una respuesta valida.");
        }

        return body.Answer;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        string? authorizationHeader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, "chat/stream", question, authorizationHeader);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var buffer = new char[1024];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0) yield break;

            yield return new string(buffer, 0, read);
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        string question,
        string? authorizationHeader)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(new RagQueryRequest(question))
        };

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        return request;
    }

    private record RagQueryRequest(string Question);

    private record RagQueryResponse(string Query, string Answer);
}
