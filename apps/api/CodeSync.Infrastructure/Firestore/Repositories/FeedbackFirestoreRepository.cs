using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Infrastructure.Firestore.Documents;
using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Repositories;

internal sealed class FeedbackFirestoreRepository : IFeedbackRepository
{
    private readonly FirestoreDb _db;
    private CollectionReference Col => _db.Collection(FirestoreCollections.Feedback);

    public FeedbackFirestoreRepository(FirestoreDb db) => _db = db;

    public async Task<IReadOnlyList<Feedback>> GetRecentByUserIdAsync(
        string userId,
        int limit = 10,
        CancellationToken ct = default)
    {
        var snap = await Col
            .WhereEqualTo("userId", userId)
            .OrderByDescending("createdAt")
            .Limit(limit)
            .GetSnapshotAsync(ct);

        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<string> CreateAsync(Feedback feedback, CancellationToken ct = default)
    {
        var doc = new FeedbackDocument
        {
            SubmissionId = feedback.SubmissionId,
            ChallengeId = feedback.ChallengeId,
            UserId = feedback.UserId,
            CoachFeedback = feedback.CoachFeedback,
            IsFallback = feedback.IsFallback,
            CreatedAt = Timestamp.FromDateTime(feedback.CreatedAt.ToUniversalTime())
        };

        var reference = await Col.AddAsync(doc, ct);
        return reference.Id;
    }

    private static Feedback ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ConvertTo<FeedbackDocument>();
        return new Feedback
        {
            Id = snap.Id,
            SubmissionId = d.SubmissionId,
            ChallengeId = d.ChallengeId,
            UserId = d.UserId,
            CoachFeedback = d.CoachFeedback,
            IsFallback = d.IsFallback,
            CreatedAt = d.CreatedAt.ToDateTime()
        };
    }
}
