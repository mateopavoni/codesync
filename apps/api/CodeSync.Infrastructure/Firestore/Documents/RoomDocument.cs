using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Documents;

[FirestoreData]
internal sealed class RoomDocument
{
    [FirestoreProperty("inviteCode")]
    public string InviteCode { get; set; } = "";

    [FirestoreProperty("challengeId")]
    public string? ChallengeId { get; set; }

    [FirestoreProperty("hostUserId")]
    public string HostUserId { get; set; } = "";

    [FirestoreProperty("memberIds")]
    public List<string> MemberIds { get; set; } = new();

    [FirestoreProperty("isActive")]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty("lastActivityAt")]
    public Timestamp LastActivityAt { get; set; }
}
