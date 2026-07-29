using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.Firestore.Seeding;

/// <summary>
/// Seeds 10 starter challenges (5 Python, 5 JavaScript) into Firestore.
/// Safe to call multiple times: checks if any challenges already exist before writing.
/// Invoke from Program.cs in development, or via a dedicated admin endpoint.
/// </summary>
public sealed class ChallengeSeeder
{
    private readonly IChallengeRepository _repo;
    private readonly ILogger<ChallengeSeeder> _logger;

    public ChallengeSeeder(IChallengeRepository repo, ILogger<ChallengeSeeder> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _repo.GetAllActiveAsync(ct);
        if (existing.Count > 0)
        {
            _logger.LogInformation("Challenges already seeded ({Count} found). Skipping.", existing.Count);
            return;
        }

        _logger.LogInformation("Seeding {Count} challenges...", SeedData.Count);

        foreach (var challenge in SeedData)
        {
            await _repo.CreateAsync(challenge, ct);
        }

        _logger.LogInformation("Seeding complete.");
    }

    private static readonly List<Challenge> SeedData = new()
    {
        // ─── PYTHON ────────────────────────────────────────────────────────────

        new Challenge
        {
            Title = "Suma de dos numeros",
            Description = "Escribe una funcion que recibe dos numeros enteros y retorna su suma.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(a, b):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[1, 2]",     ExpectedOutput = "3",   IsVisible = true },
                new() { Args = "[0, 0]",     ExpectedOutput = "0",   IsVisible = true },
                new() { Args = "[-1, 1]",    ExpectedOutput = "0",   IsVisible = false },
                new() { Args = "[100, 200]", ExpectedOutput = "300", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Revertir un string",
            Description = "Dada una cadena de texto, retorna la cadena invertida.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(s):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]",  ExpectedOutput = "\"olleh\"", IsVisible = true },
                new() { Args = "[\"world\"]",  ExpectedOutput = "\"dlrow\"", IsVisible = true },
                new() { Args = "[\"\"]",       ExpectedOutput = "\"\"",      IsVisible = false },
                new() { Args = "[\"a\"]",      ExpectedOutput = "\"a\"",     IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Encontrar el maximo",
            Description = "Dada una lista de numeros, retorna el valor maximo.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(nums):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[[1, 2, 3]]",       ExpectedOutput = "3",  IsVisible = true },
                new() { Args = "[[-1, -2, -3]]",    ExpectedOutput = "-1", IsVisible = true },
                new() { Args = "[[5]]",             ExpectedOutput = "5",  IsVisible = false },
                new() { Args = "[[3, 1, 4, 1, 5]]", ExpectedOutput = "5", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "FizzBuzz",
            Description = "Dado un numero n, retorna \"Fizz\" si es divisible por 3, \"Buzz\" si es divisible por 5, \"FizzBuzz\" si es divisible por ambos, o el numero como string si no lo es.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(n):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[3]",  ExpectedOutput = "\"Fizz\"",     IsVisible = true },
                new() { Args = "[5]",  ExpectedOutput = "\"Buzz\"",     IsVisible = true },
                new() { Args = "[15]", ExpectedOutput = "\"FizzBuzz\"", IsVisible = true },
                new() { Args = "[7]",  ExpectedOutput = "\"7\"",        IsVisible = false },
                new() { Args = "[1]",  ExpectedOutput = "\"1\"",        IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Fibonacci",
            Description = "Retorna el n-esimo numero de la secuencia de Fibonacci. F(0)=0, F(1)=1, F(n)=F(n-1)+F(n-2).",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(n):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[0]",  ExpectedOutput = "0",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "1",  IsVisible = true },
                new() { Args = "[5]",  ExpectedOutput = "5",  IsVisible = true },
                new() { Args = "[10]", ExpectedOutput = "55", IsVisible = false },
                new() { Args = "[7]",  ExpectedOutput = "13", IsVisible = false }
            }
        },

        // ─── JAVASCRIPT ────────────────────────────────────────────────────────

        new Challenge
        {
            Title = "Suma de dos numeros",
            Description = "Escribe una funcion que recibe dos numeros y retorna su suma.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(a, b) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[1, 2]",     ExpectedOutput = "3",   IsVisible = true },
                new() { Args = "[0, 0]",     ExpectedOutput = "0",   IsVisible = true },
                new() { Args = "[-1, 1]",    ExpectedOutput = "0",   IsVisible = false },
                new() { Args = "[100, 200]", ExpectedOutput = "300", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Revertir un string",
            Description = "Dada una cadena de texto, retorna la cadena invertida.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(s) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]", ExpectedOutput = "\"olleh\"", IsVisible = true },
                new() { Args = "[\"world\"]", ExpectedOutput = "\"dlrow\"", IsVisible = true },
                new() { Args = "[\"\"]",      ExpectedOutput = "\"\"",      IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Verificar palindromo",
            Description = "Dada una cadena de texto, retorna true si es un palindromo (se lee igual al derecho y al reves), false en caso contrario.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(s) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"racecar\"]", ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[\"hello\"]",   ExpectedOutput = "false", IsVisible = true },
                new() { Args = "[\"a\"]",       ExpectedOutput = "true",  IsVisible = false },
                new() { Args = "[\"abba\"]",    ExpectedOutput = "true",  IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "FizzBuzz",
            Description = "Dado un numero n, retorna \"Fizz\" si es divisible por 3, \"Buzz\" si es divisible por 5, \"FizzBuzz\" si es divisible por ambos, o el numero como string si no lo es.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(n) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[3]",  ExpectedOutput = "\"Fizz\"",     IsVisible = true },
                new() { Args = "[5]",  ExpectedOutput = "\"Buzz\"",     IsVisible = true },
                new() { Args = "[15]", ExpectedOutput = "\"FizzBuzz\"", IsVisible = true },
                new() { Args = "[7]",  ExpectedOutput = "\"7\"",        IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Fibonacci",
            Description = "Retorna el n-esimo numero de la secuencia de Fibonacci. F(0)=0, F(1)=1, F(n)=F(n-1)+F(n-2).",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(n) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[0]",  ExpectedOutput = "0",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "1",  IsVisible = true },
                new() { Args = "[5]",  ExpectedOutput = "5",  IsVisible = true },
                new() { Args = "[10]", ExpectedOutput = "55", IsVisible = false }
            }
        }
    };
}
