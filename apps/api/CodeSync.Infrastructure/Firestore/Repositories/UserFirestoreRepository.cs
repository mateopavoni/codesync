using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Infrastructure.Firestore.Documents;
using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Repositories;

internal sealed class UserFirestoreRepository : IUserRepository
{
    private readonly FirestoreDb _db;
    private CollectionReference Col => _db.Collection(FirestoreCollections.Users);

    public UserFirestoreRepository(FirestoreDb db) => _db = db;

    public async Task<User?> GetByIdAsync(string uid, CancellationToken ct = default)
    {
        var snap = await Col.Document(uid).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(uid, snap) : null;
    }

    public async Task UpsertAsync(User user, CancellationToken ct = default)
    {
        var doc = new UserDocument
        {
            DisplayName = user.DisplayName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            Xp = user.Xp,
            Level = user.Level,
            CompletedChallengeIds = user.CompletedChallengeIds,
            CreatedAt = Timestamp.FromDateTime(user.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(user.UpdatedAt.ToUniversalTime())
        };

        // SetAsync with Merge = false replaces the whole document
        await Col.Document(user.Uid).SetAsync(doc, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<User>> GetTopByXpAsync(int limit, CancellationToken ct = default)
    {
        var snap = await Col.OrderByDescending("xp").Limit(limit).GetSnapshotAsync(ct);
        return snap.Documents.Select(d => ToEntity(d.Id, d)).ToList();
    }

    private static User ToEntity(string uid, DocumentSnapshot snap)
    {
        var d = snap.ConvertTo<UserDocument>();
        return new User
        {
            Uid = uid,
            DisplayName = d.DisplayName,
            Email = d.Email,
            PhotoUrl = d.PhotoUrl,
            Xp = d.Xp,
            Level = d.Level,
            CompletedChallengeIds = d.CompletedChallengeIds,
            CreatedAt = d.CreatedAt.ToDateTime(),
            UpdatedAt = d.UpdatedAt.ToDateTime()
        };
    }
}
