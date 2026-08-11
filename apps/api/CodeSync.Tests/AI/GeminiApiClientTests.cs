using CodeSync.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace CodeSync.Tests.AI;

/// <summary>
/// Unit tests for GeminiApiClient. The real Gemini API is not called — a fake
/// HttpMessageHandler returns controlled response bodies so we can verify the
/// finishReason handling independently.
/// </summary>
public sealed class GeminiApiClientTests
{
    private sealed class FakeHandler(HttpStatusCode status, object body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(status) { Content = JsonContent.Create(body) };
            return Task.FromResult(response);
        }
    }

    private static GeminiApiClient BuildClient(HttpStatusCode status, object body)
    {
        var httpClient = new HttpClient(new FakeHandler(status, body)) { BaseAddress = new Uri("https://fake-gemini.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Gemini")).Returns(httpClient);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Gemini:ApiKey", "fake-key" } })
            .Build();

        return new GeminiApiClient(factory.Object, config, NullLogger<GeminiApiClient>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_FinishReasonStop_ReturnsText()
    {
        var body = new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = "Buen intento, revisá el caso base." } } }, finishReason = "STOP" }
            }
        };

        var result = await BuildClient(HttpStatusCode.OK, body).GenerateAsync("prompt");

        Assert.Equal("Buen intento, revisá el caso base.", result);
    }

    [Fact]
    public async Task GenerateAsync_FinishReasonMaxTokens_ReturnsNullInsteadOfTruncatedText()
    {
        var body = new
        {
            candidates = new[]
            {
                new { content = new { parts = new[] { new { text = "Parece que tus tests fallaron porque el código que escribiste está realizando una" } } }, finishReason = "MAX_TOKENS" }
            }
        };

        var result = await BuildClient(HttpStatusCode.OK, body).GenerateAsync("prompt");

        Assert.Null(result);
    }
}
