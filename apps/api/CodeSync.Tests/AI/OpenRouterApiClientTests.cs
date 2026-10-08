using CodeSync.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeSync.Tests.AI;

/// <summary>
/// Unit tests for OpenRouterApiClient. The real OpenRouter API is not called — a
/// fake HttpMessageHandler returns scripted responses and records the requests,
/// so we can verify request shape, finish_reason handling and model fallback.
/// </summary>
public sealed class OpenRouterApiClientTests
{
    private sealed class FakeHandler(params (HttpStatusCode Status, object Body)[] script) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request, body));
            var (status, payload) = script[Math.Min(Calls.Count - 1, script.Length - 1)];
            return new HttpResponseMessage(status) { Content = JsonContent.Create(payload) };
        }
    }

    private static OpenRouterApiClient BuildClient(
        FakeHandler handler,
        Dictionary<string, string?>? settings = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fake-openrouter.test") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("OpenRouter")).Returns(httpClient);

        var values = new Dictionary<string, string?> { { "OpenRouter:ApiKey", "fake-key" } };
        if (settings != null)
            foreach (var kv in settings) values[kv.Key] = kv.Value;

        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new OpenRouterApiClient(factory.Object, config, NullLogger<OpenRouterApiClient>.Instance);
    }

    private static object Completion(string text, string finishReason) => new
    {
        choices = new[]
        {
            new { message = new { role = "assistant", content = text }, finish_reason = finishReason }
        }
    };

    [Fact]
    public async Task GenerateAsync_FinishReasonStop_ReturnsText()
    {
        var handler = new FakeHandler((HttpStatusCode.OK, Completion("Buen intento, revisá el caso base.", "stop")));

        var result = await BuildClient(handler).GenerateAsync("prompt");

        Assert.Equal("Buen intento, revisá el caso base.", result);
    }

    [Fact]
    public async Task GenerateAsync_FinishReasonLength_ReturnsNullInsteadOfTruncatedText()
    {
        var handler = new FakeHandler((HttpStatusCode.OK, Completion("Tus tests fallaron porque el código está realizando una", "length")));

        var result = await BuildClient(handler).GenerateAsync("prompt");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_NoApiKey_ReturnsNullWithoutCallingTheApi()
    {
        var handler = new FakeHandler((HttpStatusCode.OK, Completion("x", "stop")));
        var client = BuildClient(handler, new() { { "OpenRouter:ApiKey", "" } });

        var result = await client.GenerateAsync("prompt");

        Assert.Null(result);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task GenerateAsync_SendsBearerKeyDefaultModelAndUserPrompt()
    {
        var handler = new FakeHandler((HttpStatusCode.OK, Completion("ok", "stop")));

        await BuildClient(handler).GenerateAsync("mi prompt");

        var (request, body) = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/chat/completions", request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("fake-key", request.Headers.Authorization!.Parameter);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("google/gemma-4-31b-it:free", doc.RootElement.GetProperty("model").GetString());
        var message = doc.RootElement.GetProperty("messages")[0];
        Assert.Equal("user", message.GetProperty("role").GetString());
        Assert.Equal("mi prompt", message.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GenerateAsync_RateLimited429_ReturnsNullAndDoesNotRetry()
    {
        // The free-tier daily quota is shared across :free models, so retrying
        // another free model on 429 would only burn a second request.
        var handler = new FakeHandler((HttpStatusCode.TooManyRequests, new { error = new { message = "rate limited" } }));

        var result = await BuildClient(handler).GenerateAsync("prompt");

        Assert.Null(result);
        Assert.Single(handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GenerateAsync_ModelUnavailable_RetriesOnceWithFallbackModel(HttpStatusCode firstStatus)
    {
        var handler = new FakeHandler(
            (firstStatus, new { error = new { message = "model gone" } }),
            (HttpStatusCode.OK, Completion("respuesta del fallback", "stop")));

        var result = await BuildClient(handler).GenerateAsync("prompt");

        Assert.Equal("respuesta del fallback", result);
        Assert.Equal(2, handler.Calls.Count);
        using var second = JsonDocument.Parse(handler.Calls[1].Body);
        Assert.Equal("openrouter/free", second.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task GenerateAsync_EmptyChoices_ReturnsNull()
    {
        var handler = new FakeHandler((HttpStatusCode.OK, new { choices = Array.Empty<object>() }));

        var result = await BuildClient(handler).GenerateAsync("prompt");

        Assert.Null(result);
    }
}
