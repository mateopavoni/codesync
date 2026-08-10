using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CodeSync.Infrastructure.AI;

public sealed class AICoachService : IAICoachService
{
    private readonly GeminiApiClient _gemini;
    private readonly IRateLimiterService _rateLimiter;
    private readonly IConfiguration _config;
    private readonly ILogger<AICoachService> _logger;

    public AICoachService(
        GeminiApiClient gemini,
        IRateLimiterService rateLimiter,
        IConfiguration config,
        ILogger<AICoachService> logger)
    {
        _gemini = gemini;
        _rateLimiter = rateLimiter;
        _config = config;
        _logger = logger;
    }

    public async Task<AICoachResponse> GetFeedbackAsync(
        string challengeId,
        string challengeTitle,
        DifficultyLevel difficulty,
        ProgrammingLanguage language,
        string code,
        IReadOnlyList<TestCaseResult> failedTests,
        string userId,
        CancellationToken ct = default)
    {
        var windowMinutes = _config.GetValue("RateLimit:AICoachWindowMinutes", 1);
        var rateLimitKey = $"aicoach:{userId}";

        var allowed = await _rateLimiter.TryConsumeAsync(
            rateLimitKey,
            TimeSpan.FromMinutes(windowMinutes),
            ct);

        if (!allowed)
        {
            _logger.LogInformation("AI Coach rate-limited for user {UserId}.", userId);
            return new AICoachResponse(
                Feedback: FallbackHintProvider.GetHint(challengeTitle, difficulty),
                IsFallback: true,
                WasRateLimited: true);
        }

        var prompt = BuildPrompt(challengeTitle, difficulty, language, code, failedTests);
        var geminiText = await _gemini.GenerateAsync(prompt, ct);

        if (geminiText == null)
        {
            _logger.LogWarning("Gemini returned null for challenge {ChallengeId}. Using fallback hint.", challengeId);
            return new AICoachResponse(
                Feedback: FallbackHintProvider.GetHint(challengeTitle, difficulty),
                IsFallback: true,
                WasRateLimited: false);
        }

        return new AICoachResponse(
            Feedback: geminiText.Trim(),
            IsFallback: false,
            WasRateLimited: false);
    }

    private static string BuildPrompt(
        string challengeTitle,
        DifficultyLevel difficulty,
        ProgrammingLanguage language,
        string code,
        IReadOnlyList<TestCaseResult> failedTests)
    {
        var langName = language switch
        {
            ProgrammingLanguage.Python => "Python",
            ProgrammingLanguage.JavaScript => "JavaScript",
            ProgrammingLanguage.Html => "HTML",
            ProgrammingLanguage.Ruby => "Ruby",
            ProgrammingLanguage.Java => "Java",
            ProgrammingLanguage.CSharp => "C#",
            _ => language.ToString()
        };
        var diffName = difficulty switch
        {
            DifficultyLevel.Easy => "facil",
            DifficultyLevel.Medium => "intermedio",
            DifficultyLevel.Hard => "dificil",
            _ => "desconocido"
        };

        var sb = new StringBuilder();
        sb.AppendLine($"Eres un coach de programacion educativo para un estudiante que esta aprendiendo {langName}.");
        sb.AppendLine($"El estudiante intento resolver el desafio \"{challengeTitle}\" (nivel: {diffName}) y algunos tests fallaron.");
        sb.AppendLine();

        sb.AppendLine("Tests fallidos:");
        foreach (var t in failedTests.Take(3)) // limit to 3 to keep prompt small
        {
            sb.AppendLine($"  - Test {t.TestCaseIndex + 1}: obtuvo '{t.ActualOutput}'{(t.Error != null ? $" (error: {t.Error})" : "")}");
        }
        sb.AppendLine();

        sb.AppendLine($"Codigo del estudiante:");
        sb.AppendLine("```" + langName.ToLower());
        // Truncate code to avoid excessive tokens
        var codeSnippet = code.Length > 1500 ? code[..1500] + "\n# ... (truncado)" : code;
        sb.AppendLine(codeSnippet);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("Proporcioná retroalimentacion concisa en español (maximo 3 parrafos cortos) que:");
        sb.AppendLine("1. Explique brevemente por que fallaron los tests.");
        sb.AppendLine("2. Sugiera como corregir el codigo SIN dar la solucion completa.");
        sb.AppendLine("3. Incluya un consejo de buenas practicas relevante.");
        sb.AppendLine("No uses markdown. Escribi en tono amigable y alentador.");

        return sb.ToString();
    }
}
