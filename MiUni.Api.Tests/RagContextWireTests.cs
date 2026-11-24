using System.Net;
using System.Text;
using System.Text.Json;
using MiUni.Api.Services;
using Xunit;

namespace MiUni.Api.Tests;

public class RagContextWireTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothProtocolsSendIdenticalTypedContextAndForwardAuthorization(bool stream)
    {
        var handler = new CaptureHandler(stream);
        var client = new RagClient(new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/api/v1/") });
        var input = new RagChatInput("y los documentos?", [new("user", "intercambios"), new("assistant", "respuesta")],
            new([new("Math", 1, "09:00", "10:00", null, null)], "lunes 2026-11-23 09:00"));
        if (stream)
        {
            var answer = new StringBuilder();
            await foreach (var chunk in client.StreamAnswerAsync(input, "Bearer test-token", default)) answer.Append(chunk);
            Assert.Equal("Answer", answer.ToString());
        }
        else Assert.Equal("Answer", await client.GetAnswerAsync(input, "Bearer test-token", default));
        using var json = JsonDocument.Parse(handler.Body!); var root = json.RootElement;
        Assert.Equal(input.Question, root.GetProperty("question").GetString());
        Assert.Equal(2, root.GetProperty("history").GetArrayLength());
        Assert.Equal("assistant", root.GetProperty("history")[1].GetProperty("role").GetString());
        var context = root.GetProperty("personal_context");
        Assert.Equal(1, context.GetProperty("schedule")[0].GetProperty("day").GetInt32());
        Assert.True(context.TryGetProperty("campus_now", out _));
        Assert.Equal("Bearer test-token", handler.Authorization);
    }

    private sealed class CaptureHandler(bool stream) : HttpMessageHandler
    {
        public string? Body; public string? Authorization;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = await request.Content!.ReadAsStringAsync(ct);
            Authorization = request.Headers.Authorization?.ToString();
            return new(HttpStatusCode.OK) { Content = new StringContent(stream ? "Answer" : "{\"query\":\"q\",\"answer\":\"Answer\"}",
                Encoding.UTF8, stream ? "text/plain" : "application/json") };
        }
    }
}
