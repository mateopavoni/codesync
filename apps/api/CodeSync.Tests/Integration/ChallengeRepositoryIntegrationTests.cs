using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using CodeSync.Infrastructure.Firestore.Repositories;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="ChallengeFirestoreRepository"/> against the Firestore emulator.
/// Each test method starts with a clean Firestore collection (cleared in InitializeAsync).
/// </summary>
[Collection("Firestore Integration")]
[Trait("Category", "Integration")]
public sealed class ChallengeRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly ChallengeFirestoreRepository _repo;

    public ChallengeRepositoryIntegrationTests(FirestoreEmulatorFixture fixture)
    {
        _fixture = fixture;
        _repo = new ChallengeFirestoreRepository(fixture.Db);
    }

    // Clear all Firestore documents before each test method.
    public Task InitializeAsync() => _fixture.ClearDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Create + Get ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAndGetById_RoundTrip_AllFieldsPreserved()
    {
        // Arrange
        var challenge = new Challenge
        {
            Title = "Suma de dos números",
            Description = "Retorna la suma de a y b.",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "suma",
            SolutionTemplate = "def suma(a, b):\n    pass",
            TestCases = new List<TestCase>
            {
                new() { Args = "[1, 2]", ExpectedOutput = "3", IsVisible = true },
                new() { Args = "[0, 0]", ExpectedOutput = "0", IsVisible = false }
            },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        var id = await _repo.CreateAsync(challenge);
        var fetched = await _repo.GetByIdAsync(id);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(id, fetched!.Id);
        Assert.Equal("Suma de dos números", fetched.Title);
        Assert.Equal("Retorna la suma de a y b.", fetched.Description);
        Assert.Equal(DifficultyLevel.Easy, fetched.Difficulty);
        Assert.Equal(ProgrammingLanguage.Python, fetched.Language);
        Assert.Equal("suma", fetched.FunctionName);
        Assert.True(fetched.IsActive);
        Assert.Equal(2, fetched.TestCases.Count);

        var visible = Assert.Single(fetched.TestCases, tc => tc.IsVisible);
        Assert.Equal("[1, 2]", visible.Args);
        Assert.Equal("3", visible.ExpectedOutput);

        var hidden = Assert.Single(fetched.TestCases, tc => !tc.IsVisible);
        Assert.Equal("[0, 0]", hidden.Args);
        Assert.Equal("0", hidden.ExpectedOutput);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync("this-id-does-not-exist");
        Assert.Null(result);
    }

    // ── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActive_ReturnsOnlyActiveChallenges()
    {
        // Create 3 challenges: 2 active, 1 that will be soft-deleted.
        var id1 = await _repo.CreateAsync(MakeChallenge("FizzBuzz", DifficultyLevel.Easy));
        var id2 = await _repo.CreateAsync(MakeChallenge("Fibonacci", DifficultyLevel.Medium));
        var id3 = await _repo.CreateAsync(MakeChallenge("Palindrome", DifficultyLevel.Hard));

        // Soft-delete the third one.
        await _repo.DeleteAsync(id3);

        var active = await _repo.GetAllActiveAsync();

        Assert.Equal(2, active.Count);
        Assert.All(active, c => Assert.True(c.IsActive));
        Assert.Contains(active, c => c.Id == id1);
        Assert.Contains(active, c => c.Id == id2);
        Assert.DoesNotContain(active, c => c.Id == id3);
    }

    // ── Soft Delete ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_DocumentStillExistsWithIsActiveFalse()
    {
        // Arrange: create an active challenge.
        var id = await _repo.CreateAsync(MakeChallenge("Target", DifficultyLevel.Easy));
        var before = await _repo.GetByIdAsync(id);
        Assert.True(before!.IsActive, "Challenge should be active before deletion.");

        // Act: soft-delete.
        await _repo.DeleteAsync(id);

        // Assert: document exists but is no longer active.
        var after = await _repo.GetByIdAsync(id);
        Assert.NotNull(after);
        Assert.False(after!.IsActive, "Soft-deleted challenge should have IsActive = false.");
        Assert.Equal("Target", after.Title); // rest of the document is intact
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Challenge MakeChallenge(string title, DifficultyLevel difficulty) => new()
    {
        Title = title,
        Description = $"Descripción de {title}.",
        Difficulty = difficulty,
        Language = ProgrammingLanguage.Python,
        FunctionName = "solution",
        SolutionTemplate = "def solution():\n    pass",
        TestCases = new List<TestCase>
        {
            new() { Args = "[]", ExpectedOutput = "null", IsVisible = true }
        },
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
}
