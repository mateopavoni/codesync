using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Documents;

[FirestoreData]
internal sealed class SubmissionDocument
{
    [FirestoreProperty("challengeId")]
    public string ChallengeId { get; set; } = "";

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = "";

    [FirestoreProperty("code")]
    public string Code { get; set; } = "";

    [FirestoreProperty("language")]
    public int Language { get; set; }

    [FirestoreProperty("status")]
    public int Status { get; set; }

    [FirestoreProperty("allTestsPassed")]
    public bool AllTestsPassed { get; set; }

    [FirestoreProperty("testResults")]
    public List<TestResultDocument> TestResults { get; set; } = new();

    [FirestoreProperty("executionTimeMs")]
    public int ExecutionTimeMs { get; set; }

    [FirestoreProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    [FirestoreProperty("submittedAt")]
    public Timestamp SubmittedAt { get; set; }
}

[FirestoreData]
internal sealed class TestResultDocument
{
    [FirestoreProperty("testCaseIndex")]
    public int TestCaseIndex { get; set; }

    [FirestoreProperty("passed")]
    public bool Passed { get; set; }

    [FirestoreProperty("actualOutput")]
    public string ActualOutput { get; set; } = "";

    [FirestoreProperty("error")]
    public string? Error { get; set; }
}
