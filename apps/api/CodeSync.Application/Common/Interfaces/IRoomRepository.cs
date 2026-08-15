using CodeSync.Domain.Entities;

namespace CodeSync.Application.Common.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Room?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    Task<string> CreateAsync(Room room, CancellationToken ct = default);
    Task<int> CountActiveByHostUserIdAsync(string hostUserId, CancellationToken ct = default);

    /// <summary>Asigna el desafío elegido para la sala (se elige adentro de la sala, no al crearla).</summary>
    Task UpdateChallengeAsync(string roomId, string challengeId, CancellationToken ct = default);

    /// <summary>
    /// Atomically adds <paramref name="userId"/> to the room identified by <paramref name="inviteCode"/>,
    /// enforcing <paramref name="maxMembers"/> inside a Firestore transaction so concurrent joins can't
    /// race past the cap. Idempotent — joining twice returns the current room unchanged.
    /// Throws <see cref="KeyNotFoundException"/> if no active room matches, <see cref="InvalidOperationException"/> if full.
    /// </summary>
    Task<Room> JoinAsync(string inviteCode, string userId, int maxMembers, CancellationToken ct = default);
}
