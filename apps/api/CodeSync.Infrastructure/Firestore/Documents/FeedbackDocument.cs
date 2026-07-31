using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Documents;

[FirestoreData]
internal sealed class FeedbackDocument
{
    [FirestoreProperty("submissionId")]
    public string SubmissionId { get; set; } = "";

    [FirestoreProperty("challengeId")]
    public string ChallengeId { get; set; } = "";

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = "";

    [FirestoreProperty("coachFeedback")]
    public string CoachFeedback { get; set; } = "";

    [FirestoreProperty("isFallback")]
    public bool IsFallback { get; set; }

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }
}
