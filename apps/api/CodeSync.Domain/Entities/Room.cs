namespace CodeSync.Domain.Entities;

public sealed class Room
{
    public string Id { get; set; } = "";

    /// <summary>Short alphanumeric code shared with collaborators. E.g. "AB12CD".</summary>
    public string InviteCode { get; set; } = "";

    /// <summary>Null hasta que alguien elige un desafío desde adentro de la sala.</summary>
    public string? ChallengeId { get; set; }
    public string HostUserId { get; set; } = "";

    /// <summary>Firebase UIDs of all members currently in the room.</summary>
    public List<string> MemberIds { get; set; } = new();

    public const int MaxMembers = 4;

    /// <summary>Cap on how many active rooms a single user can host at once —
    /// prevents one user from accumulating an unbounded number of open rooms.</summary>
    public const int MaxActiveRoomsPerHost = 3;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
