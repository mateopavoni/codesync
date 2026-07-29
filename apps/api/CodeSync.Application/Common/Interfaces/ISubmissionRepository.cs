using CodeSync.Domain.Entities;

namespace CodeSync.Application.Common.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Submission>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<string> CreateAsync(Submission submission, CancellationToken ct = default);
}
