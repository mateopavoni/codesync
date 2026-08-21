using CodeSync.Application.Common.Extensions;
using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain;
using MediatR;

namespace CodeSync.Application.Features.Dashboard.Queries.GetDashboard;

internal sealed class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IUserRepository _users;
    private readonly IChallengeRepository _challenges;
    private readonly IFeedbackRepository _feedbacks;

    public GetDashboardHandler(
        IUserRepository users,
        IChallengeRepository challenges,
        IFeedbackRepository feedbacks)
    {
        _users = users;
        _challenges = challenges;
        _feedbacks = feedbacks;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userTask = _users.GetByIdAsync(request.UserId, cancellationToken);
        var challengesTask = _challenges.GetAllActiveAsync(cancellationToken);
        var feedbackTask = _feedbacks.GetRecentByUserIdAsync(request.UserId, limit: 5, ct: cancellationToken);

        await Task.WhenAll(userTask, challengesTask, feedbackTask);

        var user = await userTask;
        var challenges = await challengesTask;
        var feedbacks = await feedbackTask;

        if (user == null)
            return new DashboardDto(
                1, 0, Array.Empty<DashboardChallengeDto>(), Array.Empty<DashboardChallengeDto>(), Array.Empty<DashboardFeedbackDto>(),
                LevelCalculator.NextLevelThreshold(0), LevelCalculator.CurrentLevelFloor(0));

        var completedIds = new HashSet<string>(user.CompletedChallengeIds);
        var challengesById = challenges.ToDictionary(c => c.Id);

        var completedChallenges = challenges
            .Where(c => completedIds.Contains(c.Id))
            .Select(c => new DashboardChallengeDto(c.Id, c.Title, c.Difficulty.ToApiString()))
            .ToList();

        var pendingChallenges = challenges
            .Where(c => !completedIds.Contains(c.Id))
            .Select(c => new DashboardChallengeDto(c.Id, c.Title, c.Difficulty.ToApiString()))
            .ToList();

        var recentFeedback = feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new DashboardFeedbackDto(
                f.Id,
                challengesById.TryGetValue(f.ChallengeId, out var ch) ? ch.Title : "Desafío",
                f.CoachFeedback,
                f.CreatedAt))
            .ToList();

        return new DashboardDto(
            user.Level, user.Xp, completedChallenges, pendingChallenges, recentFeedback,
            LevelCalculator.NextLevelThreshold(user.Xp), LevelCalculator.CurrentLevelFloor(user.Xp));
    }
}
