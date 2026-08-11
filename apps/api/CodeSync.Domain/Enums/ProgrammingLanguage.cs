namespace CodeSync.Domain.Enums;

public enum ProgrammingLanguage
{
    Python = 1,
    JavaScript = 2,

    /// <summary>Live-preview only (iframe render) — no Docker execution, no
    /// automated grading yet. See CreateSubmissionHandler's guard.</summary>
    Html = 3
}
