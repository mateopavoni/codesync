using MediatR;

namespace CodeSync.Application.Features.Users.Commands.UpsertUserProfile;

/// <summary>
/// Creates or updates a user profile in Firestore.
/// Called on first login (Angular passes the Firebase claims to initialize the profile).
/// </summary>
public sealed record UpsertUserProfileCommand(
    string Uid,
    string DisplayName,
    string Email,
    string? PhotoUrl) : IRequest;
