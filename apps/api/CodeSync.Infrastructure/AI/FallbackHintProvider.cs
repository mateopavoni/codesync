using CodeSync.Domain.Enums;

namespace CodeSync.Infrastructure.AI;

/// <summary>
/// Pre-generated hints returned when the AI rate limit is reached.
/// These are intentionally generic (by difficulty) + per-challenge overrides
/// so students always get some guidance even without a live API call.
/// </summary>
public static class FallbackHintProvider
{
    private static readonly Dictionary<string, string> _challengeHints = new()
    {
        // Challenge-specific hints keyed by a fragment of the challenge title (lowercase)
        ["fizzbuzz"] = "Usa el operador modulo (%) para determinar divisibilidad. "
            + "Asegurate de verificar primero si el numero es divisible por 15 (FizzBuzz) antes de verificar 3 o 5 por separado.",

        ["suma"] = "Verificá que estás retornando el resultado, no solo calculándolo. "
            + "La función debe terminar con 'return a + b'.",

        ["fibonacci"] = "La secuencia de Fibonacci para n=0 es 0, para n=1 es 1, y para cualquier otro n es fib(n-1) + fib(n-2). "
            + "Considerá un caso base explícito para n <= 1.",

        ["palindromo"] = "Un palíndromo es una cadena que se lee igual al derecho y al revés. "
            + "Comparó la cadena con su versión invertida. En Python: s == s[::-1]. En JavaScript: s === s.split('').reverse().join('').",

        ["revertir"] = "Para invertir un string en Python podés usar slicing: s[::-1]. "
            + "En JavaScript: s.split('').reverse().join('').",

        ["maximo"] = "Recorre la lista comparando cada elemento con el máximo actual. "
            + "En Python podés usar max(nums) directamente si no te piden implementarlo desde cero.",

        ["aplanar"] = "Usá recursión o Array.flat() en JavaScript. En Python, podés iterar y verificar si cada elemento es una lista.",

        ["primo"] = "Un número es primo si no tiene divisores entre 2 y el mismo (exclusive). "
            + "Recordá los casos borde: 0 y 1 no son primos, y 2 sí lo es.",

        ["vocales"] = "Recorré la cadena caracter por caracter y comparalo contra el conjunto de vocales (a, e, i, o, u). "
            + "Prestá atención a mayúsculas y a vocales acentuadas si el enunciado las contempla.",

        // HTML/CSS: se corrobora contra el DOM renderizado, no contra el resultado de una función.
        ["centrar"] = "Pensá en el contenedor padre (el body), no en el elemento en sí: display: flex "
            + "junto con justify-content y align-items es la forma más directa de centrar algo en ambos ejes.",

        ["tarjeta"] = "Revisá que los selectores que pide el enunciado existan tal cual en tu HTML "
            + "(la clase del contenedor y las etiquetas de cada elemento).",

        ["formulario"] = "Cada campo necesita el atributo correcto para que el selector lo encuentre: "
            + "el input de email debe ser type=\"email\" y el botón de enviar type=\"submit\".",
    };

    private static readonly Dictionary<DifficultyLevel, string> _levelHints = new()
    {
        [DifficultyLevel.Easy] =
            "Revisá la lógica básica de la función: ¿estás usando la operacion correcta? "
            + "Verificá que el valor de retorno es del tipo esperado (numero, string, booleano). "
            + "Compará tu resultado manualmente con el caso de prueba visible.",

        [DifficultyLevel.Medium] =
            "Considerá los casos borde: ¿qué pasa con entrada vacía, valores negativos o cero? "
            + "Asegurate de que todos los condicionales esten en el orden correcto. "
            + "Imprime valores intermedios para debuggear.",

        [DifficultyLevel.Hard] =
            "Pensá en la complejidad temporal. ¿Tu algoritmo es O(n) o O(n²)? "
            + "Para problemas complejos, intentá dividirlo en sub-problemas mas pequeños. "
            + "Revisá si tu caso base está bien definido (especialmente en recursión).",
    };

    private const string HtmlFallback =
        "Revisá el enunciado y fijate qué selectores CSS y atributos pide (clases, etiquetas, tipos de input). "
        + "El test corrobora el DOM final, no tu código, así que probá tu HTML en el navegador y comparalo a ojo.";

    public static string GetHint(string challengeTitle, DifficultyLevel difficulty, ProgrammingLanguage language)
    {
        var titleLower = challengeTitle.ToLowerInvariant();

        foreach (var (keyword, hint) in _challengeHints)
        {
            if (titleLower.Contains(keyword))
                return hint;
        }

        if (language is ProgrammingLanguage.Html or ProgrammingLanguage.Css)
            return HtmlFallback;

        return _levelHints.TryGetValue(difficulty, out var levelHint)
            ? levelHint
            : "Revisá tu código cuidadosamente. Comparalo con los casos de prueba visibles e intentá ejecutarlo mentalmente paso a paso.";
    }
}
