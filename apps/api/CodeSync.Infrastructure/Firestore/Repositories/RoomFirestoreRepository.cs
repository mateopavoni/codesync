using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Infrastructure.Firestore.Documents;
using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Repositories;

internal sealed class RoomFirestoreRepository : IRoomRepository
{
    private readonly FirestoreDb _db;
    private CollectionReference Col => _db.Collection(FirestoreCollections.Rooms);

    public RoomFirestoreRepository(FirestoreDb db) => _db = db;

    public async Task<Room?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var snap = await Col.Document(id).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<Room?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default)
    {
        var snap = await Col
            .WhereEqualTo("inviteCode", inviteCode)
            .WhereEqualTo("isActive", true)
            .Limit(1)
            .GetSnapshotAsync(ct);

        if (snap.Documents.Count == 0) return null;

        var room = ToEntity(snap.Documents[0]);
        return room.IsExpired ? null : room;
    }

    public async Task<string> CreateAsync(Room room, CancellationToken ct = default)
    {
        var doc = ToDocument(room);
        var reference = await Col.AddAsync(doc, ct);
        return reference.Id;
    }

    public async Task<int> CountActiveByHostUserIdAsync(string hostUserId, CancellationToken ct = default)
    {
        var snap = await Col
            .WhereEqualTo("hostUserId", hostUserId)
            .WhereEqualTo("isActive", true)
            .GetSnapshotAsync(ct);

        // Filtro de expiracion en memoria (a lo sumo MaxActiveRoomsPerHost documentos por
        // host, no vale la pena un indice compuesto solo para esto).
        return snap.Documents.Select(ToEntity).Count(r => !r.IsExpired);
    }

    public async Task UpdateChallengeAsync(string roomId, string challengeId, CancellationToken ct = default)
    {
        await Col.Document(roomId).UpdateAsync(new Dictionary<string, object>
        {
            { "challengeId", challengeId },
            { "lastActivityAt", Timestamp.FromDateTime(DateTime.UtcNow) }
        }, cancellationToken: ct);
    }

    public async Task CloseAsync(string roomId, CancellationToken ct = default)
    {
        await Col.Document(roomId).UpdateAsync(new Dictionary<string, object>
        {
            { "isActive", false }
        }, cancellationToken: ct);
    }

    public Task<Room> JoinAsync(string inviteCode, string userId, int maxMembers, CancellationToken ct = default)
    {
        var query = Col.WhereEqualTo("inviteCode", inviteCode).WhereEqualTo("isActive", true).Limit(1);

        return _db.RunTransactionAsync(async transaction =>
        {
            var querySnap = await transaction.GetSnapshotAsync(query, ct);
            if (querySnap.Count == 0)
                throw new KeyNotFoundException("No existe una sala activa con ese código de invitación.");

            var docSnap = querySnap[0];
            var room = ToEntity(docSnap);

            if (room.IsExpired)
                throw new KeyNotFoundException("No existe una sala activa con ese código de invitación.");

            if (room.MemberIds.Contains(userId))
                return room;

            if (room.MemberIds.Count >= maxMembers)
                throw new InvalidOperationException($"La sala está llena (máximo {maxMembers} usuarios).");

            room.MemberIds.Add(userId);
            room.LastActivityAt = DateTime.UtcNow;
            transaction.Set(docSnap.Reference, ToDocument(room));
            return room;
        }, cancellationToken: ct);
    }

    private static RoomDocument ToDocument(Room r) => new()
    {
        InviteCode = r.InviteCode,
        ChallengeId = r.ChallengeId,
        HostUserId = r.HostUserId,
        MemberIds = r.MemberIds,
        IsActive = r.IsActive,
        CreatedAt = Timestamp.FromDateTime(r.CreatedAt.ToUniversalTime()),
        LastActivityAt = Timestamp.FromDateTime(r.LastActivityAt.ToUniversalTime())
    };

    private static Room ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ConvertTo<RoomDocument>();
        var createdAt = d.CreatedAt.ToDateTime();
        return new Room
        {
            Id = snap.Id,
            InviteCode = d.InviteCode,
            ChallengeId = d.ChallengeId,
            HostUserId = d.HostUserId,
            MemberIds = d.MemberIds,
            IsActive = d.IsActive,
            CreatedAt = createdAt,
            // Docs viejos (pre-expiracion) no tienen este campo — Timestamp default cae en
            // epoch 1970, lo que los marcaria como expirados al toque. Fallback a CreatedAt.
            LastActivityAt = d.LastActivityAt.ToDateTime() > createdAt ? d.LastActivityAt.ToDateTime() : createdAt
        };
    }
}
