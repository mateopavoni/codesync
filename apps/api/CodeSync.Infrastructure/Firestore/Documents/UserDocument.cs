using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Documents;

[FirestoreData]
internal sealed class UserDocument
{
    [FirestoreProperty("displayName")]
    public string DisplayName { get; set; } = "";

    [FirestoreProperty("email")]
    public string Email { get; set; } = "";

    [FirestoreProperty("photoUrl")]
    public string? PhotoUrl { get; set; }

    [FirestoreProperty("xp")]
    public int Xp { get; set; }

    [FirestoreProperty("level")]
    public int Level { get; set; } = 1;

    [FirestoreProperty("completedChallengeIds")]
    public List<string> CompletedChallengeIds { get; set; } = new();

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public Timestamp UpdatedAt { get; set; }
}
