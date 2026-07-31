using CodeSync.Domain.Entities;

namespace CodeSync.Application.Common.Interfaces;

public interface IFeedbackRepository
{
    Task<IReadOnlyList<Feedback>> GetRecentByUserIdAsync(string userId, int limit = 10, CancellationToken ct = default);
    Task<string> CreateAsync(Feedback feedback, CancellationToken ct = default);
}
