using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CodeSync.Infrastructure.AI;

/// <summary>
/// Thin HTTP client for OpenRouter's OpenAI-compatible chat completions API.
/// Uses a named HttpClient ("OpenRouter") registered in DI with the base URL already set.
/// Authentication is a Bearer API key (a free key works with the ":free" models).
/// </summary>
public sealed class OpenRouterApiClient
{
    private const string DefaultModel = "google/gemma-4-31b-it:free";
    private const string DefaultFallbackModel = "openrouter/free";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenRouterApiClient> _logger;

    public OpenRouterApiClient(IHttpClientFactory factory, IConfiguration config, ILogger<OpenRouterApiClient> logger)
    {
        _http = factory.CreateClient("OpenRouter");
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends a single-turn prompt and returns the model's text response.
    /// Returns null if the API call fails (caller decides fallback strategy).
    /// </summary>
    public async Task<string?> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = _config["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenRouter:ApiKey is not configured. Skipping AI Coach call.");
            return null;
        }

        var model = _config["OpenRouter:Model"] ?? DefaultModel;
        var fallbackModel = _config["OpenRouter:FallbackModel"] ?? DefaultFallbackModel;

        try
        {
            var (status, text) = await SendAsync(apiKey, model, prompt, ct);

            // Free models get withdrawn without notice (404) or have upstream
            // outages (5xx): retry once with the router-picked free model.
            // 429 is NOT retried — the free daily quota is shared across ":free"
            // models, so a second request would only burn more of it.
            if ((status == HttpStatusCode.NotFound || (int)status >= 500) && fallbackModel != model)
            {
                _logger.LogWarning("OpenRouter model {Model} unavailable ({Status}); retrying with {Fallback}.",
                    model, (int)status, fallbackModel);
                (_, text) = await SendAsync(apiKey, fallbackModel, prompt, ct);
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenRouter API.");
            return null;
        }
    }

    private async Task<(HttpStatusCode Status, string? Text)> SendAsync(
        string apiKey, string model, string prompt, CancellationToken ct)
    {
        var requestBody = new ChatRequest
        {
            Model = model,
            Messages = new[] { new ChatMessage { Role = "user", Content = prompt } },
            Temperature = 0.7f,
            MaxTokens = 1024
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("OpenRouter API returned {Status}: {Body}", (int)response.StatusCode, body[..Math.Min(200, body.Length)]);
            return (response.StatusCode, null);
        }

        var parsed = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
        var choice = parsed?.Choices?.FirstOrDefault();

        // The model hit max_tokens mid-sentence: the text is real but cut off.
        // Showing that half-finished fragment to a student is worse than the
        // canned fallback hint, so treat it the same as "no response" and let
        // the caller (AICoachService) fall back.
        if (choice?.FinishReason == "length")
        {
            _logger.LogWarning("OpenRouter response truncated at max_tokens.");
            return (response.StatusCode, null);
        }

        return (response.StatusCode, choice?.Message?.Content);
    }

    // --- OpenRouter (OpenAI-compatible) request/response types ---

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required ChatMessage[] Messages { get; init; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; init; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; init; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = "user";

        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; init; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; init; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }
    }
}
