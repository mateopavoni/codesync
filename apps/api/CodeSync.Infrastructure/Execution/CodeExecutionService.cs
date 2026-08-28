using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CodeSync.Infrastructure.Execution;

/// <summary>
/// Builds a language-specific test runner, hands it to DockerExecutor,
/// and interprets the JSON output against the challenge's test cases.
/// </summary>
public sealed class CodeExecutionService : ICodeExecutionService
{
    private readonly IDockerExecutor _docker;
    private readonly IConfiguration _config;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(
        IDockerExecutor docker,
        IConfiguration config,
        ILogger<CodeExecutionService> logger)
    {
        _docker = docker;
        _config = config;
        _logger = logger;
    }

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code,
        string functionName,
        ProgrammingLanguage language,
        IReadOnlyList<TestCase> testCases,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var (image, command) = GetContainerSpec(language);
        // HTML cold-starts a full Chromium inside the container on top of
        // the container-start cost every other language already pays — 5s is too
        // tight for that, 12s leaves headroom without letting a stuck submission
        // hold the sandbox much longer than the others.
        int timeout = language is ProgrammingLanguage.Html or ProgrammingLanguage.Css
            ? _config.GetValue("Docker:HtmlExecutionTimeoutSeconds", 12)
            : _config.GetValue("Docker:ExecutionTimeoutSeconds", 5);

        var testCasesJson = JsonSerializer.Serialize(
            testCases.Select((tc, i) => new
            {
                index = i,
                args = tc.Args,
                expectedOutput = tc.ExpectedOutput
            }));

        var fullProgram = BuildRunner(language, functionName, code, testCasesJson);

        _logger.LogDebug("Running {Language} submission — image={Image}, testCases={Count}",
            language, image, testCases.Count);

        // Chromium alone eats ~150-300MB RSS just to launch a single tab — the
        // 256MB default other languages use risks a silent OOM-kill for HTML.
        // PidsLimit also needs headroom: cgroups counts *threads*, not just
        // processes, and a single headless Chromium instance easily spins up
        // 100+ threads across its browser/GPU/renderer processes — confirmed by
        // hand (50 reliably hit "pthread_create: Resource temporarily
        // unavailable"; 300 leaves real headroom while still being nowhere near
        // what an actual fork bomb would attempt).
        var (memoryBytes, tmpfsSize, pidsLimit) = language is ProgrammingLanguage.Html or ProgrammingLanguage.Css
            ? (512 * 1024 * 1024L, "64m", 300L)
            : (256 * 1024 * 1024L, "8m", 50L);

        var result = await _docker.RunAsync(image, command, fullProgram, timeout, memoryBytes, tmpfsSize, pidsLimit, ct);
        sw.Stop();

        if (result.TimedOut)
        {
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: BuildTimeoutResults(testCases.Count, timeout),
                TimedOut: true,
                Error: null,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        if (result.Error != null)
        {
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: Array.Empty<TestCaseResult>(),
                TimedOut: false,
                Error: result.Error,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        // Stdout should be a JSON array of test results from our runner
        var stdout = result.Stdout.Trim();
        if (string.IsNullOrEmpty(stdout) && !string.IsNullOrEmpty(result.Stderr))
        {
            // Interpreter error (syntax, runtime before any output)
            var errorSnippet = TruncateError(result.Stderr);
            _logger.LogDebug("Interpreter error: {Stderr}", result.Stderr);
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: BuildErrorResults(testCases.Count, errorSnippet),
                TimedOut: false,
                Error: errorSnippet,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        return ParseRunnerOutput(stdout, result.Stderr, sw.ElapsedMilliseconds);
    }

    private static CodeExecutionResult ParseRunnerOutput(string stdout, string stderr, long elapsedMs)
    {
        try
        {
            // student code (console.log/print) can write to the same stdout
            // before the harness does. The harness always prints its JSON as the last
            // line, so take the last non-empty line instead of the whole stream —
            // everything before it is the student's own output, surfaced separately
            // as ConsoleOutput instead of being discarded.
            // Ceiling: breaks if student code schedules output *after* returning
            // (e.g. a stray setTimeout) — not a case the sync harness produces today.
            var lines = stdout.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
            var lastNonEmptyIndex = lines.FindLastIndex(line => line.Trim().Length > 0);
            var jsonLine = lastNonEmptyIndex >= 0 ? lines[lastNonEmptyIndex].Trim() : stdout;
            var consoleOutput = lastNonEmptyIndex > 0
                ? string.Join('\n', lines.Take(lastNonEmptyIndex)).Trim()
                : null;
            if (string.IsNullOrEmpty(consoleOutput)) consoleOutput = null;

            var runnerResults = JsonSerializer.Deserialize<List<RunnerResult>>(
                jsonLine,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Empty runner output.");

            var testResults = runnerResults.Select((r, i) => new TestCaseResult(
                i,
                r.Passed,
                r.ActualOutput ?? "",
                r.Error)).ToList();

            return new CodeExecutionResult(
                AllTestsPassed: testResults.All(r => r.Passed),
                TestResults: testResults,
                TimedOut: false,
                Error: null,
                ExecutionTimeMs: (int)elapsedMs,
                ConsoleOutput: consoleOutput);
        }
        catch (Exception ex)
        {
            var detail = string.IsNullOrEmpty(stderr)
                ? $"Failed to parse test runner output: {ex.Message}"
                : TruncateError(stderr);

            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: Array.Empty<TestCaseResult>(),
                TimedOut: false,
                Error: detail,
                ExecutionTimeMs: (int)elapsedMs);
        }
    }

    private static (string image, IReadOnlyList<string> command) GetContainerSpec(ProgrammingLanguage lang) =>
        lang switch
        {
            ProgrammingLanguage.Python => (
                "python:3.12-alpine",
                new[] { "python3", "-" }),
            ProgrammingLanguage.JavaScript => (
                "node:22-alpine",
                new[] { "node", "--input-type=commonjs" }),
            // the *official* Playwright image ships Chromium preinstalled
            // but not the npm package itself (it's meant as a base you `npm ci` your
            // own project onto) — with NetworkMode=none inside the execution
            // container, a bare `require('playwright')` would fail with "Cannot find
            // module". `docker/html-runner.Dockerfile` layers playwright-core on top
            // at build time (when network is available) — build it once per host,
            // see that file's header comment.
            // Css shares the same runner/image as Html — see ProgrammingLanguage.Css's
            // doc comment.
            ProgrammingLanguage.Html or ProgrammingLanguage.Css => (
                "codesync-html-runner:1.48.0",
                new[] { "node", "--input-type=commonjs" }),
            ProgrammingLanguage.Ruby => (
                "ruby:3.3-alpine",
                new[] { "ruby", "-" }),
            // Java's JEP 330 single-file launcher compiles+runs in one JVM
            // process (no exec of a file written to the noexec /tmp), so a plain
            // "java <file>" works here — unlike Go, see ProgrammingLanguage.cs.
            ProgrammingLanguage.Java => (
                "eclipse-temurin:21-jdk-alpine",
                new[] { "sh -c 'cat > /tmp/Main.java && java /tmp/Main.java'" }),
            // "dotnet run" needs a NuGet restore (network, unavailable
            // here) even for a zero-dependency file. Compiling straight through the
            // SDK's own csc.dll against its bundled ref pack, then running the DLL
            // with the plain "dotnet" muxer, stays fully offline. Paths are
            // discovered at runtime (not hardcoded) so an SDK image bump doesn't
            // silently break this. -maxdepth 4 on the csc.dll search (sdk/<ver>/Roslyn/
            // bincore/csc.dll) — an unbounded find was walking the whole SDK tree
            // (thousands of files) on every single submission.
            ProgrammingLanguage.CSharp => (
                "mcr.microsoft.com/dotnet/sdk:10.0",
                new[]
                {
                    """
                    sh -c 'CSC=$(find /usr/share/dotnet/sdk -maxdepth 4 -name csc.dll | head -1)
                    REF=$(find /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref -maxdepth 2 -mindepth 2 -type d | sort -V | tail -1)/net10.0
                    RTVER=$(basename "$(find /usr/share/dotnet/shared/Microsoft.NETCore.App -maxdepth 1 -mindepth 1 -type d | sort -V | tail -1)")
                    cat > /tmp/Program.cs
                    REFS=""
                    for f in "$REF"/*.dll; do REFS="$REFS -r:$f"; done
                    printf "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"%s\"}}}" "$RTVER" > /tmp/Program.runtimeconfig.json
                    dotnet "$CSC" -nologo -nowarn:8632 -optimize+ -langversion:latest -nostdlib+ -noconfig $REFS -out:/tmp/Program.dll -target:exe /tmp/Program.cs 1>&2
                    dotnet /tmp/Program.dll'
                    """
                }),
            _ => throw new NotSupportedException($"Language '{lang}' is not supported for execution.")
        };

    private static string BuildRunner(
        ProgrammingLanguage language,
        string functionName,
        string studentCode,
        string testCasesJson) =>
        language switch
        {
            ProgrammingLanguage.Python => BuildPythonRunner(functionName, studentCode, testCasesJson),
            ProgrammingLanguage.JavaScript => BuildJavaScriptRunner(functionName, studentCode, testCasesJson),
            // HTML/CSS have no target function to call — testCasesJson carries DOM
            // assertions instead of args/expectedOutput pairs to diff.
            ProgrammingLanguage.Html or ProgrammingLanguage.Css => BuildHtmlRunner(studentCode, testCasesJson),
            ProgrammingLanguage.Ruby => BuildRubyRunner(functionName, studentCode, testCasesJson),
            ProgrammingLanguage.Java => BuildJavaRunner(functionName, studentCode, testCasesJson),
            ProgrammingLanguage.CSharp => BuildCSharpRunner(functionName, studentCode, testCasesJson),
            _ => throw new NotSupportedException($"No runner template for language '{language}'.")
        };

    private static string BuildPythonRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        import json, sys, traceback

        # --- Student code ---
        {{code}}

        # --- Test runner (injected by CodeSync) ---
        _test_cases = {{testCasesJson}}
        _results = []
        for _tc in _test_cases:
            try:
                _args = json.loads(_tc['args'])
                if not isinstance(_args, list):
                    _args = [_args]
                _actual = {{functionName}}(*_args)
                _expected = json.loads(_tc['expectedOutput'])
                _results.append({'passed': _actual == _expected, 'actualOutput': json.dumps(_actual)})
            except Exception as _e:
                _results.append({'passed': False, 'actualOutput': '', 'error': str(_e)})

        print(json.dumps(_results))
        sys.stdout.flush()
        """;

    private static string BuildJavaScriptRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        // --- Student code ---
        {{code}}

        // --- Test runner (injected by CodeSync) ---
        const _testCases = {{testCasesJson}};
        const _results = _testCases.map(_tc => {
            try {
                const _args = JSON.parse(_tc.args);
                const _argList = Array.isArray(_args) ? _args : [_args];
                const _actual = {{functionName}}(..._argList);
                const _expected = JSON.parse(_tc.expectedOutput);
                return { passed: JSON.stringify(_actual) === JSON.stringify(_expected), actualOutput: JSON.stringify(_actual) };
            } catch(_e) {
                return { passed: false, actualOutput: '', error: _e.message };
            }
        });
        process.stdout.write(JSON.stringify(_results) + '\n');
        """;

    // HTML has no function to call, so unlike the runners above it treats the
    // student's markup as *data*, not code: it's loaded into a real (sandboxed,
    // network-less) Chromium tab via page.setContent, and each TestCase.ExpectedOutput
    // is one DOM assertion (see ProgrammingLanguage.Html's doc comment for the
    // supported types) evaluated against the rendered page — never an exact-string
    // diff, so formatting/order differences in the student's HTML don't fail them.
    private static string BuildHtmlRunner(string code, string testCasesJson) =>
        $$"""
        const { chromium } = require('playwright-core');

        // Serialized server-side with JsonSerializer so this is always a valid,
        // safely-escaped JS string literal no matter what the student's HTML
        // contains (backticks, ${...}, </script>, etc).
        const _studentHtml = {{JsonSerializer.Serialize(code)}};
        const _testCases = {{testCasesJson}};

        (async () => {
          const browser = await chromium.launch({ args: ['--no-sandbox', '--disable-dev-shm-usage'] });
          const page = await browser.newPage();
          page.on('dialog', d => d.dismiss());
          // Segunda capa de defensa (la primera es NetworkMode=none del container):
          // corta cualquier fetch/recurso externo a nivel de Playwright en vez de
          // dejar que el propio timeout HTTP de una request sin red se coma tiempo.
          await page.route('**/*', route => route.abort());

          try {
            await page.setContent(_studentHtml, { waitUntil: 'domcontentloaded', timeout: 3000 });
          } catch (e) {
            console.log(JSON.stringify(_testCases.map(() => ({ passed: false, actualOutput: '', error: 'setup_timeout: ' + e.message }))));
            await browser.close();
            process.exit(0);
          }

          const results = [];
          for (const tc of _testCases) {
            try {
              const assertion = JSON.parse(tc.expectedOutput);
              const evalPromise = page.evaluate((a) => {
                const els = Array.from(document.querySelectorAll(a.selector));
                switch (a.type) {
                  case 'exists':
                    return { ok: els.length > 0, actual: els.length + ' elemento(s)' };
                  case 'textContains': {
                    const ok = els.some((el) => (el.textContent || '').includes(a.value));
                    return { ok, actual: els.map((el) => el.textContent).join(' | ').slice(0, 200) };
                  }
                  case 'attribute': {
                    const ok = els.some((el) => el.getAttribute(a.attr) === a.value);
                    return { ok, actual: els.map((el) => el.getAttribute(a.attr)).join(', ') };
                  }
                  case 'count': {
                    const n = els.length;
                    const cmp = a.comparator || 'eq';
                    const ok = cmp === 'eq' ? n === a.value : cmp === 'gte' ? n >= a.value : cmp === 'lte' ? n <= a.value : false;
                    return { ok, actual: String(n) };
                  }
                  case 'centered': {
                    if (els.length === 0) return { ok: false, actual: 'no existe' };
                    const r = els[0].getBoundingClientRect();
                    const cx = r.left + r.width / 2;
                    const cy = r.top + r.height / 2;
                    const dx = Math.abs(cx - window.innerWidth / 2);
                    const dy = Math.abs(cy - window.innerHeight / 2);
                    const tolerance = a.tolerance || 20;
                    return { ok: dx <= tolerance && dy <= tolerance, actual: 'centro en (' + Math.round(cx) + ',' + Math.round(cy) + ')' };
                  }
                  default:
                    return { ok: false, actual: '' };
                }
              }, assertion);
              const timeoutGuard = new Promise((_, rej) => setTimeout(() => rej(new Error('assertion_timeout')), 2000));
              const r = await Promise.race([evalPromise, timeoutGuard]);
              results.push({ passed: r.ok, actualOutput: r.actual, error: null });
            } catch (e) {
              results.push({ passed: false, actualOutput: '', error: e.message });
            }
          }

          console.log(JSON.stringify(results));
          await browser.close();
          process.exit(0);
        })().catch((e) => {
          console.log(JSON.stringify([{ passed: false, actualOutput: '', error: 'harness_error: ' + e.message }]));
          process.exit(0);
        });
        """;

    private static string BuildRubyRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        require 'json'

        # --- Student code ---
        {{code}}

        # --- Test runner (injected by CodeSync) ---
        # raw JSON dropped straight into a single-quoted Ruby string. Safe
        # because JSON only ever uses double quotes, so it can't collide with the
        # single-quote delimiter (same trick BuildPythonRunner relies on for dict/
        # list literal syntax — Ruby just needs the explicit JSON.parse call).
        _test_cases = JSON.parse('{{testCasesJson}}')
        _results = _test_cases.map do |_tc|
          begin
            _args = JSON.parse(_tc['args'])
            _args = [_args] unless _args.is_a?(Array)
            _actual = send(:{{functionName}}, *_args)
            _expected = JSON.parse(_tc['expectedOutput'])
            { passed: _actual == _expected, actualOutput: _actual.to_json }
          rescue => _e
            { passed: false, actualOutput: '', error: _e.message }
          end
        end
        puts _results.to_json
        """;

    // Java/C# are statically typed, so unlike the dynamic-language
    // runners above we can't just splat parsed JSON args into the student's
    // function — we have to look up its declared parameter types via reflection
    // and coerce each JSON value to match. That's what the Json/reflection
    // scaffolding in both templates below is for.

    private static string BuildJavaRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        import java.lang.reflect.*;
        import java.util.*;
        import java.util.stream.*;

        public class Main {
            // --- Student code ---
            {{code}}

            // --- Test runner (injected by CodeSync) ---
            @SuppressWarnings("unchecked")
            public static void main(String[] _rawArgs) {
                List<Object> _testCases = (List<Object>) Json.parse("{{EscapeForCLikeStringLiteral(testCasesJson)}}");
                List<Object> _results = new ArrayList<>();
                Method _method = _findMethod("{{functionName}}");
                for (Object _tcObj : _testCases) {
                    Map<String, Object> _tc = (Map<String, Object>) _tcObj;
                    Map<String, Object> _result = new LinkedHashMap<>();
                    try {
                        Object _argsParsed = Json.parse((String) _tc.get("args"));
                        List<Object> _argList = _argsParsed instanceof List
                            ? (List<Object>) _argsParsed
                            : Collections.singletonList(_argsParsed);
                        Object[] _callArgs = _coerceArgs(_method, _argList);
                        Object _actual = _method.invoke(null, _callArgs);
                        Object _expected = Json.parse((String) _tc.get("expectedOutput"));
                        Object _actualNorm = _normalize(_actual);
                        boolean _passed = _deepEquals(_actualNorm, _expected);
                        _result.put("passed", _passed);
                        _result.put("actualOutput", Json.stringify(_actualNorm));
                    } catch (Throwable _e) {
                        Throwable _cause = _e instanceof InvocationTargetException ? _e.getCause() : _e;
                        _result.put("passed", false);
                        _result.put("actualOutput", "");
                        _result.put("error", _cause.getMessage() == null ? _cause.toString() : _cause.getMessage());
                    }
                    _results.add(_result);
                }
                System.out.println(Json.stringify(_results));
            }

            private static Method _findMethod(String name) {
                for (Method m : Main.class.getDeclaredMethods()) {
                    if (m.getName().equals(name)) {
                        m.setAccessible(true);
                        return m;
                    }
                }
                throw new RuntimeException("Metodo no encontrado: " + name);
            }

            private static Object[] _coerceArgs(Method m, List<Object> argList) {
                Class<?>[] types = m.getParameterTypes();
                Type[] genTypes = m.getGenericParameterTypes();
                Object[] out = new Object[types.length];
                for (int i = 0; i < types.length; i++) out[i] = _coerce(argList.get(i), types[i], genTypes[i]);
                return out;
            }

            @SuppressWarnings("unchecked")
            private static Object _coerce(Object v, Class<?> type, Type genType) {
                if (v == null) return null;
                if (type == int.class || type == Integer.class) return ((Number) v).intValue();
                if (type == long.class || type == Long.class) return ((Number) v).longValue();
                if (type == double.class || type == Double.class) return ((Number) v).doubleValue();
                if (type == float.class || type == Float.class) return ((Number) v).floatValue();
                if (type == short.class || type == Short.class) return ((Number) v).shortValue();
                if (type == boolean.class || type == Boolean.class || type == String.class) return v;
                if (type.isArray()) {
                    List<Object> list = (List<Object>) v;
                    Class<?> comp = type.getComponentType();
                    Object arr = Array.newInstance(comp, list.size());
                    for (int i = 0; i < list.size(); i++) Array.set(arr, i, _coerce(list.get(i), comp, comp));
                    return arr;
                }
                if (List.class.isAssignableFrom(type)) {
                    List<Object> list = (List<Object>) v;
                    Class<?> elemType = Object.class;
                    if (genType instanceof ParameterizedType pt) {
                        Type arg = pt.getActualTypeArguments()[0];
                        if (arg instanceof Class<?> c) elemType = c;
                    }
                    List<Object> outList = new ArrayList<>();
                    for (Object item : list) outList.add(elemType == Object.class ? item : _coerce(item, elemType, elemType));
                    return outList;
                }
                return v;
            }

            @SuppressWarnings("unchecked")
            private static Object _normalize(Object v) {
                if (v == null) return null;
                if (v instanceof Number || v instanceof String || v instanceof Boolean) return v;
                if (v.getClass().isArray()) {
                    int len = Array.getLength(v);
                    List<Object> list = new ArrayList<>();
                    for (int i = 0; i < len; i++) list.add(_normalize(Array.get(v, i)));
                    return list;
                }
                if (v instanceof List<?> l) {
                    List<Object> list = new ArrayList<>();
                    for (Object item : l) list.add(_normalize(item));
                    return list;
                }
                if (v instanceof Map<?, ?> m) {
                    Map<String, Object> map = new LinkedHashMap<>();
                    for (Map.Entry<?, ?> e : m.entrySet()) map.put(String.valueOf(e.getKey()), _normalize(e.getValue()));
                    return map;
                }
                return v.toString();
            }

            private static boolean _deepEquals(Object a, Object b) {
                if (a == null || b == null) return a == b;
                if (a instanceof Number && b instanceof Number) return ((Number) a).doubleValue() == ((Number) b).doubleValue();
                if (a instanceof List && b instanceof List) {
                    List<?> la = (List<?>) a, lb = (List<?>) b;
                    if (la.size() != lb.size()) return false;
                    for (int i = 0; i < la.size(); i++) if (!_deepEquals(la.get(i), lb.get(i))) return false;
                    return true;
                }
                if (a instanceof Map && b instanceof Map) {
                    Map<?, ?> ma = (Map<?, ?>) a, mb = (Map<?, ?>) b;
                    if (!ma.keySet().equals(mb.keySet())) return false;
                    for (Object k : ma.keySet()) if (!_deepEquals(ma.get(k), mb.get(k))) return false;
                    return true;
                }
                return a.equals(b);
            }

            // Minimal JSON parser/serializer — no external deps reach the sandboxed
            // container (network is off), so this can't be Jackson/Gson.
            static final class Json {
                private final String s;
                private int i;

                private Json(String s) { this.s = s; }

                static Object parse(String s) {
                    Json p = new Json(s);
                    p.ws();
                    return p.value();
                }

                private void ws() { while (i < s.length() && Character.isWhitespace(s.charAt(i))) i++; }

                private Object value() {
                    char c = s.charAt(i);
                    if (c == '{') return object();
                    if (c == '[') return array();
                    if (c == '"') return string();
                    if (c == 't') { i += 4; return Boolean.TRUE; }
                    if (c == 'f') { i += 5; return Boolean.FALSE; }
                    if (c == 'n') { i += 4; return null; }
                    return number();
                }

                private Map<String, Object> object() {
                    Map<String, Object> map = new LinkedHashMap<>();
                    i++; ws();
                    if (s.charAt(i) == '}') { i++; return map; }
                    while (true) {
                        ws();
                        String key = string();
                        ws(); i++; ws(); // ':'
                        map.put(key, value());
                        ws();
                        if (s.charAt(i) == ',') { i++; continue; }
                        i++; break; // '}'
                    }
                    return map;
                }

                private List<Object> array() {
                    List<Object> list = new ArrayList<>();
                    i++; ws();
                    if (s.charAt(i) == ']') { i++; return list; }
                    while (true) {
                        ws();
                        list.add(value());
                        ws();
                        if (s.charAt(i) == ',') { i++; continue; }
                        i++; break; // ']'
                    }
                    return list;
                }

                private String string() {
                    StringBuilder sb = new StringBuilder();
                    i++; // opening quote
                    while (s.charAt(i) != '"') {
                        char c = s.charAt(i);
                        if (c == '\\') {
                            i++;
                            char e = s.charAt(i);
                            switch (e) {
                                case 'n' -> sb.append('\n');
                                case 't' -> sb.append('\t');
                                case 'r' -> sb.append('\r');
                                case 'b' -> sb.append('\b');
                                case 'f' -> sb.append('\f');
                                case 'u' -> { sb.append((char) Integer.parseInt(s.substring(i + 1, i + 5), 16)); i += 4; }
                                default -> sb.append(e);
                            }
                        } else sb.append(c);
                        i++;
                    }
                    i++; // closing quote
                    return sb.toString();
                }

                private Double number() {
                    int start = i;
                    while (i < s.length() && "-+.eE0123456789".indexOf(s.charAt(i)) >= 0) i++;
                    return Double.parseDouble(s.substring(start, i));
                }

                static String stringify(Object v) {
                    StringBuilder sb = new StringBuilder();
                    write(v, sb);
                    return sb.toString();
                }

                private static void write(Object v, StringBuilder sb) {
                    if (v == null) { sb.append("null"); return; }
                    if (v instanceof String str) {
                        sb.append('"');
                        for (int j = 0; j < str.length(); j++) {
                            char c = str.charAt(j);
                            switch (c) {
                                case '"' -> sb.append("\\\"");
                                case '\\' -> sb.append("\\\\");
                                case '\n' -> sb.append("\\n");
                                case '\t' -> sb.append("\\t");
                                case '\r' -> sb.append("\\r");
                                default -> sb.append(c);
                            }
                        }
                        sb.append('"');
                        return;
                    }
                    if (v instanceof Boolean || v instanceof Number) { sb.append(v); return; }
                    if (v instanceof List<?> list) {
                        sb.append('[');
                        for (int j = 0; j < list.size(); j++) {
                            if (j > 0) sb.append(',');
                            write(list.get(j), sb);
                        }
                        sb.append(']');
                        return;
                    }
                    if (v instanceof Map<?, ?> map) {
                        sb.append('{');
                        int j = 0;
                        for (Map.Entry<?, ?> e : map.entrySet()) {
                            if (j++ > 0) sb.append(',');
                            write(String.valueOf(e.getKey()), sb);
                            sb.append(':');
                            write(e.getValue(), sb);
                        }
                        sb.append('}');
                        return;
                    }
                    write(v.toString(), sb);
                }
            }
        }
        """;

    private static string BuildCSharpRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        using System;
        using System.Linq;
        using System.Reflection;
        using System.Text.Json;
        using System.Collections.Generic;

        class Program
        {
            // --- Student code ---
            {{code}}

            // --- Test runner (injected by CodeSync) ---
            static void Main()
            {
                var testCases = JsonDocument.Parse("{{EscapeForCLikeStringLiteral(testCasesJson)}}").RootElement;
                var results = new List<object>();
                var method = typeof(Program).GetMethod(
                    "{{functionName}}",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var tc in testCases.EnumerateArray())
                {
                    try
                    {
                        var argsEl = JsonDocument.Parse(tc.GetProperty("args").GetString()!).RootElement;
                        var argElems = argsEl.ValueKind == JsonValueKind.Array
                            ? argsEl.EnumerateArray().ToArray()
                            : new[] { argsEl };
                        var ps = method!.GetParameters();
                        var argValues = new object?[ps.Length];
                        for (int i = 0; i < ps.Length; i++)
                            argValues[i] = argElems[i].Deserialize(ps[i].ParameterType);

                        var actual = method.Invoke(null, argValues);
                        var actualJson = JsonSerializer.Serialize(actual);
                        var expectedEl = JsonDocument.Parse(tc.GetProperty("expectedOutput").GetString()!).RootElement;
                        var actualEl = JsonDocument.Parse(actualJson).RootElement;
                        bool passed = JsonElement.DeepEquals(actualEl, expectedEl);
                        results.Add(new { passed, actualOutput = actualJson });
                    }
                    catch (Exception e)
                    {
                        var msg = (e is TargetInvocationException tie ? tie.InnerException ?? e : e).Message;
                        results.Add(new { passed = false, actualOutput = "", error = msg });
                    }
                }

                Console.WriteLine(JsonSerializer.Serialize(results));
            }
        }
        """;

    // Java/C# both use "..." as their string-literal delimiter, same as JSON's own
    // string syntax — so raw JSON text (which BuildPythonRunner/BuildRubyRunner
    // splice in unescaped) has to be escaped before it can sit inside one of their
    // string literals.
    private static string EscapeForCLikeStringLiteral(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static IReadOnlyList<TestCaseResult> BuildTimeoutResults(int count, int timeoutSeconds) =>
        Enumerable.Range(0, count)
            .Select(i => new TestCaseResult(i, false, "", $"Tiempo de ejecucion excedido ({timeoutSeconds}s)."))
            .ToList();

    private static IReadOnlyList<TestCaseResult> BuildErrorResults(int count, string error) =>
        Enumerable.Range(0, count)
            .Select(i => new TestCaseResult(i, false, "", error))
            .ToList();

    private static string TruncateError(string stderr, int maxLength = 500) =>
        stderr.Length <= maxLength ? stderr.Trim() : stderr.Trim()[..maxLength] + "...";

    private sealed class RunnerResult
    {
        public bool Passed { get; set; }
        public string? ActualOutput { get; set; }
        public string? Error { get; set; }
    }
}
