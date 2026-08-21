using CodeSync.Domain.Entities;

namespace CodeSync.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string uid, CancellationToken ct = default);
    Task UpsertAsync(User user, CancellationToken ct = default);

    /// <summary>Top users by XP descending, for the global leaderboard.</summary>
    Task<IReadOnlyList<User>> GetTopByXpAsync(int limit, CancellationToken ct = default);
}
