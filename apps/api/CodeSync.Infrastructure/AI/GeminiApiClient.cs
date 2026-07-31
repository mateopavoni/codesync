using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeSync.Infrastructure.AI;

/// <summary>
/// Thin HTTP client for the Gemini REST API.
/// Uses a named HttpClient ("Gemini") registered in DI with the base URL already set.
/// Authentication is via API key as a query parameter (Gemini free-tier flow).
/// </summary>
public sealed class GeminiApiClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiApiClient> _logger;

    public GeminiApiClient(IHttpClientFactory factory, IConfiguration config, ILogger<GeminiApiClient> logger)
    {
        _http = factory.CreateClient("Gemini");
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends a single-turn prompt and returns the model's text response.
    /// Returns null if the API call fails (caller decides fallback strategy).
    /// </summary>
    public async Task<string?> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini:ApiKey is not configured. Skipping AI Coach call.");
            return null;
        }

        var requestUrl = $"/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new GeminiRequest
        {
            Contents = new[]
            {
                new Content
                {
                    Role = "user",
                    Parts = new[] { new Part { Text = prompt } }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.7f,
                MaxOutputTokens = 600
            }
        };

        try
        {
            var response = await _http.PostAsJsonAsync(requestUrl, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini API returned {Status}: {Body}", response.StatusCode, body[..Math.Min(200, body.Length)]);
                return null;
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);

            return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API.");
            return null;
        }
    }

    // --- Gemini REST API request/response types ---

    private sealed class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public required Content[] Contents { get; init; }

        [JsonPropertyName("generationConfig")]
        public GenerationConfig? GenerationConfig { get; init; }
    }

    private sealed class Content
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = "user";

        [JsonPropertyName("parts")]
        public required Part[] Parts { get; init; }
    }

    private sealed class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = "";
    }

    private sealed class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; init; } = 0.7f;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; init; } = 600;
    }

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[]? Candidates { get; init; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; init; }
    }
}
