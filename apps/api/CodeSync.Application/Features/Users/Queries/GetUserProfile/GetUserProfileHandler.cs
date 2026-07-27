using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _users;

    public GetUserProfileHandler(IUserRepository users) => _users = users;

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Uid, cancellationToken)
            ?? throw new KeyNotFoundException($"User profile '{request.Uid}' not found.");

        return new UserProfileDto(
            user.Uid,
            user.DisplayName,
            user.Email,
            user.PhotoUrl,
            user.Level,
            user.CompletedChallengeIds.Count,
            user.CreatedAt);
    }
}
