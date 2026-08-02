namespace CodeSync.Tests.Integration;

/// <summary>
/// xUnit collection that shares one FirestoreEmulatorFixture across all integration test classes.
/// Tests in the same collection run SEQUENTIALLY — no concurrent emulator access.
/// </summary>
[CollectionDefinition("Firestore Integration")]
public sealed class FirestoreIntegrationCollection
    : ICollectionFixture<FirestoreEmulatorFixture>
{
    // No members required — the [CollectionDefinition] attribute is what xUnit reads.
}
