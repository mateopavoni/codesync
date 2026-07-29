using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Documents;

[FirestoreData]
internal sealed class ChallengeDocument
{
    [FirestoreProperty("title")]
    public string Title { get; set; } = "";

    [FirestoreProperty("description")]
    public string Description { get; set; } = "";

    [FirestoreProperty("difficulty")]
    public int Difficulty { get; set; }

    [FirestoreProperty("language")]
    public int Language { get; set; }

    [FirestoreProperty("functionName")]
    public string FunctionName { get; set; } = "solution";

    [FirestoreProperty("solutionTemplate")]
    public string SolutionTemplate { get; set; } = "";

    [FirestoreProperty("testCases")]
    public List<TestCaseDocument> TestCases { get; set; } = new();

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty("isActive")]
    public bool IsActive { get; set; } = true;
}

[FirestoreData]
internal sealed class TestCaseDocument
{
    [FirestoreProperty("args")]
    public string Args { get; set; } = "[]";

    [FirestoreProperty("expectedOutput")]
    public string ExpectedOutput { get; set; } = "null";

    [FirestoreProperty("isVisible")]
    public bool IsVisible { get; set; } = true;
}
