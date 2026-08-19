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

    /// <summary>Se actualiza al unirse alguien o elegir desafío — sirve para detectar salas abandonadas.</summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    // ponytail: expira sola a las 24hs sin actividad para no acumular salas
    // fantasma que ocupen el cupo de MaxActiveRoomsPerHost. Sin job de limpieza:
    // se chequea al leer (lazy). Si hace falta ajustar el tiempo, es esta constante.
    public static readonly TimeSpan InactivityTimeout = TimeSpan.FromHours(24);

    public bool IsExpired => DateTime.UtcNow - LastActivityAt > InactivityTimeout;

    public bool IsUsable => IsActive && !IsExpired;
}
