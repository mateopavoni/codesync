using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Users.Commands.UpsertUserProfile;

internal sealed class UpsertUserProfileHandler : IRequestHandler<UpsertUserProfileCommand>
{
    private readonly IUserRepository _users;

    public UpsertUserProfileHandler(IUserRepository users) => _users = users;

    public async Task Handle(UpsertUserProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByIdAsync(request.Uid, cancellationToken);

        var user = existing ?? new User
        {
            Uid = request.Uid,
            CreatedAt = DateTime.UtcNow,
            Level = 1,
            CompletedChallengeIds = new List<string>()
        };

        // Always update mutable profile fields (display name / photo can change in Firebase Auth)
        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.PhotoUrl = request.PhotoUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.UpsertAsync(user, cancellationToken);
    }
}
