namespace CodeSync.Domain.Entities;

public sealed class Room
{
    public string Id { get; set; } = "";

    /// <summary>Short alphanumeric code shared with collaborators. E.g. "AB12CD".</summary>
    public string InviteCode { get; set; } = "";

    public string ChallengeId { get; set; } = "";
    public string HostUserId { get; set; } = "";

    /// <summary>Firebase UIDs of all members currently in the room.</summary>
    public List<string> MemberIds { get; set; } = new();

    public const int MaxMembers = 4;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
