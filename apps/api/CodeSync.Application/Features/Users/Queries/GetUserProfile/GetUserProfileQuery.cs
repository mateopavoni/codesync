using MediatR;

namespace CodeSync.Application.Features.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(string Uid) : IRequest<UserProfileDto>;

public sealed record UserProfileDto(
    string Uid,
    string DisplayName,
    string Email,
    string? PhotoUrl,
    int Level,
    int CompletedChallengeCount,
    DateTime CreatedAt);
