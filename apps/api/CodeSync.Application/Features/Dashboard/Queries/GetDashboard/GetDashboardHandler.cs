using CodeSync.Application.Common.Interfaces;
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
            return new DashboardDto(0, challenges.Count, Array.Empty<string>(), Array.Empty<RecentFeedbackDto>(), 1);

        var recentFeedback = feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new RecentFeedbackDto(f.Id, f.ChallengeId, f.CoachFeedback, f.IsFallback, f.CreatedAt))
            .ToList();

        return new DashboardDto(
            user.CompletedChallengeIds.Count,
            challenges.Count,
            user.CompletedChallengeIds,
            recentFeedback,
            user.Level);
    }
}
