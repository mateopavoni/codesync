using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Leaderboard.Queries.GetLeaderboard;

internal sealed class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardDto>
{
    private readonly IUserRepository _users;

    public GetLeaderboardHandler(IUserRepository users) => _users = users;

    public async Task<LeaderboardDto> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var top = await _users.GetTopByXpAsync(request.Limit, cancellationToken);

        var entries = top
            .Select((u, i) => new LeaderboardEntryDto(i + 1, u.Uid, u.DisplayName, u.PhotoUrl, u.Xp, u.Level))
            .ToList();

        return new LeaderboardDto(entries);
    }
}
