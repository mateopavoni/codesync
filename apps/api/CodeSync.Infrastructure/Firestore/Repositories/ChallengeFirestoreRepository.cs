using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using CodeSync.Infrastructure.Firestore.Documents;
using Google.Cloud.Firestore;

namespace CodeSync.Infrastructure.Firestore.Repositories;

internal sealed class ChallengeFirestoreRepository : IChallengeRepository
{
    private readonly FirestoreDb _db;
    private CollectionReference Col => _db.Collection(FirestoreCollections.Challenges);

    public ChallengeFirestoreRepository(FirestoreDb db) => _db = db;

    public async Task<Challenge?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var snap = await Col.Document(id).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<IReadOnlyList<Challenge>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var query = Col.WhereEqualTo("isActive", true);
        var snap = await query.GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<string> CreateAsync(Challenge challenge, CancellationToken ct = default)
    {
        var doc = ToDocument(challenge);
        var reference = await Col.AddAsync(doc, ct);
        return reference.Id;
    }

    public async Task UpdateAsync(Challenge challenge, CancellationToken ct = default)
    {
        var doc = ToDocument(challenge);
        await Col.Document(challenge.Id).SetAsync(doc, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        // Soft delete: mark isActive = false
        await Col.Document(id).UpdateAsync(new Dictionary<string, object>
        {
            { "isActive", false }
        }, cancellationToken: ct);
    }

    private static ChallengeDocument ToDocument(Challenge c) => new()
    {
        Title = c.Title,
        Description = c.Description,
        Difficulty = (int)c.Difficulty,
        Language = (int)c.Language,
        FunctionName = c.FunctionName,
        SolutionTemplate = c.SolutionTemplate,
        TestCases = c.TestCases.Select(tc => new TestCaseDocument
        {
            Args = tc.Args,
            ExpectedOutput = tc.ExpectedOutput,
            IsVisible = tc.IsVisible
        }).ToList(),
        CreatedAt = Timestamp.FromDateTime(c.CreatedAt.ToUniversalTime()),
        IsActive = c.IsActive
    };

    private static Challenge ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ConvertTo<ChallengeDocument>();
        return new Challenge
        {
            Id = snap.Id,
            Title = d.Title,
            Description = d.Description,
            Difficulty = (DifficultyLevel)d.Difficulty,
            Language = (ProgrammingLanguage)d.Language,
            FunctionName = d.FunctionName,
            SolutionTemplate = d.SolutionTemplate,
            TestCases = d.TestCases.Select(tc => new TestCase
            {
                Args = tc.Args,
                ExpectedOutput = tc.ExpectedOutput,
                IsVisible = tc.IsVisible
            }).ToList(),
            CreatedAt = d.CreatedAt.ToDateTime(),
            IsActive = d.IsActive
        };
    }
}
