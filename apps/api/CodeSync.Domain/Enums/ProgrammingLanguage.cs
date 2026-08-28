namespace CodeSync.Domain.Enums;

public enum ProgrammingLanguage
{
    Python = 1,
    JavaScript = 2,

    /// <summary>Graded in a headless-Chromium Docker sandbox (Playwright): the
    /// student's markup is loaded via page.setContent and checked against DOM
    /// assertions (element exists / text contains / attribute / count / centered).
    /// See CodeExecutionService.BuildHtmlRunner.</summary>
    Html = 3,

    Ruby = 4,
    Java = 5,
    CSharp = 6,

    /// <summary>Same grading path as <see cref="Html"/> (same Docker image and
    /// BuildHtmlRunner) — split into its own value only so /desafios can list
    /// HTML and CSS as separate categories with separate icons.</summary>
    Css = 7

    // Go was evaluated and dropped for now — `go run` compiles a real
    // ELF binary and execve()s it, which needs an executable tmpfs. The other
    // languages here either interpret (Ruby) or JIT in-process (Java, C#), so
    // they run fine under DockerExecutor's noexec /tmp. Go also needs ~4s just
    // to compile "fmt" from a cold, per-container build cache — over half the
    // default 5s execution timeout before the student's code even runs. Add it
    // when there's a prebuilt image with a warm GOCACHE baked in.
}
