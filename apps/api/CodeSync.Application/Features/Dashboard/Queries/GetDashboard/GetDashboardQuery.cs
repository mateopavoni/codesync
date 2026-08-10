using MediatR;

namespace CodeSync.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery(string UserId) : IRequest<DashboardDto>;

public sealed record DashboardDto(
    int Level,
    IReadOnlyList<DashboardChallengeDto> CompletedChallenges,
    IReadOnlyList<DashboardChallengeDto> PendingChallenges,
    IReadOnlyList<DashboardFeedbackDto> RecentFeedback);

public sealed record DashboardChallengeDto(string Id, string Title, string Difficulty);

public sealed record DashboardFeedbackDto(
    string Id,
    string ChallengeTitle,
    string Message,
    DateTime CreatedAt);
