using CodeSync.Domain.Entities;

namespace CodeSync.Application.Common.Interfaces;

public interface IChallengeRepository
{
    Task<Challenge?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Challenge>> GetAllActiveAsync(CancellationToken ct = default);
    Task<string> CreateAsync(Challenge challenge, CancellationToken ct = default);
    Task UpdateAsync(Challenge challenge, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
