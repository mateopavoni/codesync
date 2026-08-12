using CodeSync.Application.Common.Extensions;
using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenges;

internal sealed class GetChallengesHandler : IRequestHandler<GetChallengesQuery, IReadOnlyList<ChallengeSummaryDto>>
{
    private readonly IChallengeRepository _repo;
    private readonly IUserRepository _users;
    private readonly ISubmissionRepository _submissions;

    public GetChallengesHandler(IChallengeRepository repo, IUserRepository users, ISubmissionRepository submissions)
    {
        _repo = repo;
        _users = users;
        _submissions = submissions;
    }

    public async Task<IReadOnlyList<ChallengeSummaryDto>> Handle(
        GetChallengesQuery request,
        CancellationToken cancellationToken)
    {
        var challenges = await _repo.GetAllActiveAsync(cancellationToken);
        var (completedIds, attemptedIds) = await GetUserProgressAsync(request.UserId, cancellationToken);

        return challenges
            .Where(c => string.IsNullOrEmpty(request.Difficulty) || c.Difficulty.ToApiString() == request.Difficulty)
            .Where(c => string.IsNullOrEmpty(request.Language) || c.Language.ToApiString() == request.Language)
            .OrderBy(c => c.Difficulty)
            .ThenBy(c => c.Title)
            .Select(c => new ChallengeSummaryDto(
                c.Id,
                c.Title,
                c.Description,
                c.Difficulty.ToApiString(),
                c.Language.ToApiString(),
                c.TestCases.Count(tc => tc.IsVisible),
                completedIds.Contains(c.Id) ? "completed" : attemptedIds.Contains(c.Id) ? "in_progress" : "not_started"))
            .ToList();
    }

    private async Task<(HashSet<string> Completed, HashSet<string> Attempted)> GetUserProgressAsync(
        string? userId, CancellationToken ct)
    {
        if (userId == null) return (new HashSet<string>(), new HashSet<string>());

        var userTask = _users.GetByIdAsync(userId, ct);
        // ponytail: últimos 50 intentos alcanza para detectar "en curso"; si algún día hay
        // usuarios con >50 intentos sin completar, cambiar por una query dedicada por challengeId.
        var submissionsTask = _submissions.GetByUserIdAsync(userId, ct);
        await Task.WhenAll(userTask, submissionsTask);

        var completed = new HashSet<string>((await userTask)?.CompletedChallengeIds ?? []);
        var attempted = (await submissionsTask).Select(s => s.ChallengeId).ToHashSet();
        return (completed, attempted);
    }
}
