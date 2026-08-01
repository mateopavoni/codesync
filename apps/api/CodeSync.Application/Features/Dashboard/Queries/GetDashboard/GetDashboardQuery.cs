using MediatR;

namespace CodeSync.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery(string UserId) : IRequest<DashboardDto>;

public sealed record DashboardDto(
    int CompletedChallengeCount,
    int TotalChallengeCount,
    IReadOnlyList<string> CompletedChallengeIds,
    IReadOnlyList<RecentFeedbackDto> RecentFeedback,
    int Level);

public sealed record RecentFeedbackDto(
    string FeedbackId,
    string ChallengeId,
    string FeedbackText,
    bool IsFallback,
    DateTime CreatedAt);
