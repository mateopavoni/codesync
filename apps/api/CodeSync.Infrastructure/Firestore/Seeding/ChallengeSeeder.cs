using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.Firestore.Seeding;

/// <summary>
/// Seeds starter challenges (Python, JavaScript, Ruby, Java, C#, plus a few
/// practice-only HTML ones) into Firestore.
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

        // ponytail: migracion de una sola vez — los primeros 6 challenges se
        // sembraron con el mismo titulo en Python y JavaScript (se veian
        // "duplicados" en la UI). Renombra en Firestore los docs viejos para
        // que coincidan con los titulos nuevos de SeedData; se puede borrar
        // este bloque una vez confirmado que corrio contra el ambiente real.
        foreach (var (oldTitle, language, newTitle) in LegacyTitleRenames)
        {
            var stale = existing.FirstOrDefault(c => c.Title == oldTitle && c.Language == language);
            if (stale is not null)
            {
                stale.Title = newTitle;
                await _repo.UpdateAsync(stale, ct);
            }
        }

        var existingKeys = existing
            .Select(c => (c.Title, c.Language))
            .ToHashSet();

        var missing = SeedData
            .Where(c => !existingKeys.Contains((c.Title, c.Language)))
            .ToList();

        if (missing.Count == 0)
        {
            _logger.LogInformation("Challenges already seeded ({Count} found). Skipping.", existing.Count);
            return;
        }

        _logger.LogInformation("Seeding {Count} missing challenges...", missing.Count);

        foreach (var challenge in missing)
        {
            await _repo.CreateAsync(challenge, ct);
        }

        _logger.LogInformation("Seeding complete.");
    }

    private static readonly List<(string OldTitle, ProgrammingLanguage Language, string NewTitle)> LegacyTitleRenames = new()
    {
        ("Suma de dos numeros", ProgrammingLanguage.Python, "Suma de dos numeros (Python)"),
        ("Suma de dos numeros", ProgrammingLanguage.JavaScript, "Suma de dos numeros (JavaScript)"),
        ("Revertir un string", ProgrammingLanguage.Python, "Revertir un string (Python)"),
        ("Revertir un string", ProgrammingLanguage.JavaScript, "Revertir un string (JavaScript)"),
        ("FizzBuzz", ProgrammingLanguage.Python, "FizzBuzz (Python)"),
        ("FizzBuzz", ProgrammingLanguage.JavaScript, "FizzBuzz (JavaScript)"),
        ("Fibonacci", ProgrammingLanguage.Python, "Fibonacci (Python)"),
        ("Fibonacci", ProgrammingLanguage.JavaScript, "Fibonacci (JavaScript)"),
        ("Contar vocales", ProgrammingLanguage.Python, "Contar vocales (Python)"),
        ("Contar vocales", ProgrammingLanguage.JavaScript, "Contar vocales (JavaScript)"),
        ("Es primo", ProgrammingLanguage.Python, "Es primo (Python)"),
        ("Es primo", ProgrammingLanguage.JavaScript, "Es primo (JavaScript)"),
    };

    private static readonly List<Challenge> SeedData = new()
    {
        // ─── PYTHON ────────────────────────────────────────────────────────────

        new Challenge
        {
            Title = "Suma de dos numeros (Python)",
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
            Title = "Revertir un string (Python)",
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
            Title = "FizzBuzz (Python)",
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
            Title = "Fibonacci (Python)",
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

        new Challenge
        {
            Title = "Contar vocales (Python)",
            Description = "Dada una cadena de texto, retorna la cantidad de vocales (a, e, i, o, u) que contiene, sin distinguir mayusculas de minusculas.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(s):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]",       ExpectedOutput = "2", IsVisible = true },
                new() { Args = "[\"CodeSync\"]",    ExpectedOutput = "2", IsVisible = true },
                new() { Args = "[\"\"]",            ExpectedOutput = "0", IsVisible = false },
                new() { Args = "[\"aeiouAEIOU\"]",  ExpectedOutput = "10", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Es primo (Python)",
            Description = "Dado un numero entero n, retorna true si es un numero primo, false en caso contrario.",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(n):\n    pass",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[2]",  ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[17]", ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[15]", ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[97]", ExpectedOutput = "true",  IsVisible = false }
            }
        },

        // ─── JAVASCRIPT ────────────────────────────────────────────────────────

        new Challenge
        {
            Title = "Suma de dos numeros (JavaScript)",
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
            Title = "Revertir un string (JavaScript)",
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
            Title = "FizzBuzz (JavaScript)",
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
            Title = "Fibonacci (JavaScript)",
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
        },

        new Challenge
        {
            Title = "Contar vocales (JavaScript)",
            Description = "Dada una cadena de texto, retorna la cantidad de vocales (a, e, i, o, u) que contiene, sin distinguir mayusculas de minusculas.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(s) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]",      ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"CodeSync\"]",   ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"\"]",           ExpectedOutput = "0",  IsVisible = false },
                new() { Args = "[\"aeiouAEIOU\"]", ExpectedOutput = "10", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Es primo (JavaScript)",
            Description = "Dado un numero entero n, retorna true si es un numero primo, false en caso contrario.",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.JavaScript,
            FunctionName = "solution",
            SolutionTemplate = "function solution(n) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[2]",  ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[17]", ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[15]", ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[97]", ExpectedOutput = "true",  IsVisible = false }
            }
        },

        // ─── RUBY ──────────────────────────────────────────────────────────

        new Challenge
        {
            Title = "Suma de dos numeros (Ruby)",
            Description = "Escribe una funcion que recibe dos numeros enteros y retorna su suma.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Ruby,
            FunctionName = "solution",
            SolutionTemplate = "def solution(a, b)\n  \nend",
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
            Title = "Revertir un string (Ruby)",
            Description = "Dada una cadena de texto, retorna la cadena invertida.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Ruby,
            FunctionName = "solution",
            SolutionTemplate = "def solution(s)\n  \nend",
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
            Title = "FizzBuzz (Ruby)",
            Description = "Dado un numero n, retorna \"Fizz\" si es divisible por 3, \"Buzz\" si es divisible por 5, \"FizzBuzz\" si es divisible por ambos, o el numero como string si no lo es.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.Ruby,
            FunctionName = "solution",
            SolutionTemplate = "def solution(n)\n  \nend",
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
            Title = "Contar vocales (Ruby)",
            Description = "Dada una cadena de texto, retorna la cantidad de vocales (a, e, i, o, u) que contiene, sin distinguir mayusculas de minusculas.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Ruby,
            FunctionName = "solution",
            SolutionTemplate = "def solution(s)\n  \nend",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]",       ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"CodeSync\"]",    ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"\"]",            ExpectedOutput = "0",  IsVisible = false },
                new() { Args = "[\"aeiouAEIOU\"]",  ExpectedOutput = "10", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Es primo (Ruby)",
            Description = "Dado un numero entero n, retorna true si es un numero primo, false en caso contrario.",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.Ruby,
            FunctionName = "solution",
            SolutionTemplate = "def solution(n)\n  \nend",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[2]",  ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[17]", ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[15]", ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[97]", ExpectedOutput = "true",  IsVisible = false }
            }
        },

        // ─── JAVA ──────────────────────────────────────────────────────────
        // Java es tipado estatico: las firmas ya declaran los tipos de los
        // parametros/retorno, a diferencia de los stubs de Python/JS/Ruby.

        new Challenge
        {
            Title = "Suma de dos numeros (Java)",
            Description = "Escribe una funcion que recibe dos numeros enteros y retorna su suma.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Java,
            FunctionName = "solution",
            SolutionTemplate = "public static int solution(int a, int b) {\n    \n}",
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
            Title = "Revertir un string (Java)",
            Description = "Dada una cadena de texto, retorna la cadena invertida.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Java,
            FunctionName = "solution",
            SolutionTemplate = "public static String solution(String s) {\n    \n}",
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
            Title = "FizzBuzz (Java)",
            Description = "Dado un numero n, retorna \"Fizz\" si es divisible por 3, \"Buzz\" si es divisible por 5, \"FizzBuzz\" si es divisible por ambos, o el numero como string si no lo es.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.Java,
            FunctionName = "solution",
            SolutionTemplate = "public static String solution(int n) {\n    \n}",
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
            Title = "Contar vocales (Java)",
            Description = "Dada una cadena de texto, retorna la cantidad de vocales (a, e, i, o, u) que contiene, sin distinguir mayusculas de minusculas.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Java,
            FunctionName = "solution",
            SolutionTemplate = "public static int solution(String s) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[\"hello\"]",       ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"CodeSync\"]",    ExpectedOutput = "2",  IsVisible = true },
                new() { Args = "[\"\"]",            ExpectedOutput = "0",  IsVisible = false },
                new() { Args = "[\"aeiouAEIOU\"]",  ExpectedOutput = "10", IsVisible = false }
            }
        },

        new Challenge
        {
            Title = "Es primo (Java)",
            Description = "Dado un numero entero n, retorna true si es un numero primo, false en caso contrario.",
            Difficulty = DifficultyLevel.Hard,
            Language = ProgrammingLanguage.Java,
            FunctionName = "solution",
            SolutionTemplate = "public static boolean solution(int n) {\n    \n}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>
            {
                new() { Args = "[2]",  ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[17]", ExpectedOutput = "true",  IsVisible = true },
                new() { Args = "[1]",  ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[15]", ExpectedOutput = "false", IsVisible = false },
                new() { Args = "[97]", ExpectedOutput = "true",  IsVisible = false }
            }
        },

        // ─── HTML (solo preview en vivo, sin calificación automática todavía) ────

        new Challenge
        {
            Title = "Tarjeta de perfil",
            Description = "Armá una tarjeta simple con una imagen, un nombre y una descripción. Es un espacio de práctica libre: no hay tests automáticos, solo mirá cómo se ve en el preview en vivo.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Html,
            FunctionName = "",
            SolutionTemplate = "<div class=\"card\">\n  <h2>Tu nombre</h2>\n  <p>Una breve descripción acá.</p>\n</div>\n\n<style>\n  .card {\n    font-family: sans-serif;\n    padding: 16px;\n    border: 1px solid #ccc;\n    border-radius: 8px;\n  }\n</style>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>()
        },

        new Challenge
        {
            Title = "Centrar un div",
            Description = "Centrá el cuadro azul vertical y horizontalmente dentro de la pantalla usando CSS. Espacio de práctica libre: no hay tests automáticos.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Html,
            FunctionName = "",
            SolutionTemplate = "<div class=\"box\"></div>\n\n<style>\n  body {\n    height: 100vh;\n    margin: 0;\n  }\n  .box {\n    width: 80px;\n    height: 80px;\n    background: steelblue;\n  }\n</style>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>()
        },

        new Challenge
        {
            Title = "Formulario de contacto",
            Description = "Armá un formulario con campos de nombre, email y mensaje, más un botón de enviar. Espacio de práctica libre: no hay tests automáticos.",
            Difficulty = DifficultyLevel.Medium,
            Language = ProgrammingLanguage.Html,
            FunctionName = "",
            SolutionTemplate = "<form>\n  <label>Nombre</label>\n  <input type=\"text\" name=\"nombre\" />\n\n  <label>Email</label>\n  <input type=\"email\" name=\"email\" />\n\n  <label>Mensaje</label>\n  <textarea name=\"mensaje\"></textarea>\n\n  <button type=\"submit\">Enviar</button>\n</form>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TestCases = new List<TestCase>()
        }
    };
}
