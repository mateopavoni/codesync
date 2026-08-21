using MediatR;

namespace CodeSync.Application.Features.Leaderboard.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int Limit = 20) : IRequest<LeaderboardDto>;

public sealed record LeaderboardDto(IReadOnlyList<LeaderboardEntryDto> Entries);

public sealed record LeaderboardEntryDto(
    int Rank,
    string Uid,
    string DisplayName,
    string? PhotoUrl,
    int Xp,
    int Level);
