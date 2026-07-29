using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using CodeSync.Infrastructure.Firestore.Documents;
using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Repositories;

internal sealed class SubmissionFirestoreRepository : ISubmissionRepository
{
    private readonly FirestoreDb _db;
    private CollectionReference Col => _db.Collection(FirestoreCollections.Submissions);

    public SubmissionFirestoreRepository(FirestoreDb db) => _db = db;

    public async Task<Submission?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var snap = await Col.Document(id).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<IReadOnlyList<Submission>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var snap = await Col
            .WhereEqualTo("userId", userId)
            .OrderByDescending("submittedAt")
            .Limit(50)
            .GetSnapshotAsync(ct);

        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<string> CreateAsync(Submission submission, CancellationToken ct = default)
    {
        var doc = ToDocument(submission);
        var reference = await Col.AddAsync(doc, ct);
        return reference.Id;
    }

    private static SubmissionDocument ToDocument(Submission s) => new()
    {
        ChallengeId = s.ChallengeId,
        UserId = s.UserId,
        Code = s.Code,
        Language = (int)s.Language,
        Status = (int)s.Status,
        AllTestsPassed = s.AllTestsPassed,
        TestResults = s.TestResults.Select(r => new TestResultDocument
        {
            TestCaseIndex = r.TestCaseIndex,
            Passed = r.Passed,
            ActualOutput = r.ActualOutput,
            Error = r.Error
        }).ToList(),
        ExecutionTimeMs = s.ExecutionTimeMs,
        ErrorMessage = s.ErrorMessage,
        SubmittedAt = Timestamp.FromDateTime(s.SubmittedAt.ToUniversalTime())
    };

    private static Submission ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ConvertTo<SubmissionDocument>();
        return new Submission
        {
            Id = snap.Id,
            ChallengeId = d.ChallengeId,
            UserId = d.UserId,
            Code = d.Code,
            Language = (ProgrammingLanguage)d.Language,
            Status = (SubmissionStatus)d.Status,
            AllTestsPassed = d.AllTestsPassed,
            TestResults = d.TestResults.Select(r => new SubmissionTestResult
            {
                TestCaseIndex = r.TestCaseIndex,
                Passed = r.Passed,
                ActualOutput = r.ActualOutput,
                Error = r.Error
            }).ToList(),
            ExecutionTimeMs = d.ExecutionTimeMs,
            ErrorMessage = d.ErrorMessage,
            SubmittedAt = d.SubmittedAt.ToDateTime()
        };
    }
}
