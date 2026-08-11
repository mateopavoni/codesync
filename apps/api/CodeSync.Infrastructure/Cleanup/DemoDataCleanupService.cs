using CodeSync.Infrastructure.Firestore;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.Cleanup;

/// <summary>
/// Borra periódicamente submissions y salas viejas de Firestore. Este es un
/// proyecto demo público — sin esto, el sandbox de Docker y Firestore
/// acumulan datos indefinidamente con cada visitante.
/// </summary>
internal sealed class DemoDataCleanupService : BackgroundService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<DemoDataCleanupService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retention;

    public DemoDataCleanupService(FirestoreDb db, IConfiguration configuration, ILogger<DemoDataCleanupService> logger)
    {
        _db = db;
        _logger = logger;
        _interval = TimeSpan.FromHours(configuration.GetValue<double?>("DemoCleanup:IntervalHours") ?? 1);
        _retention = TimeSpan.FromHours(configuration.GetValue<double?>("DemoCleanup:RetentionHours") ?? 24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo data cleanup pass failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.Subtract(_retention));

        var deletedSubmissions = await DeleteOlderThanAsync(
            FirestoreCollections.Submissions, "submittedAt", cutoff, ct);
        var deletedRooms = await DeleteOlderThanAsync(
            FirestoreCollections.Rooms, "createdAt", cutoff, ct);
        var deletedFeedback = await DeleteOlderThanAsync(
            FirestoreCollections.Feedback, "createdAt", cutoff, ct);

        if (deletedSubmissions > 0 || deletedRooms > 0 || deletedFeedback > 0)
        {
            _logger.LogInformation(
                "Demo cleanup: {Submissions} submissions, {Rooms} rooms, {Feedback} feedback deleted (retention {Hours}h).",
                deletedSubmissions, deletedRooms, deletedFeedback, _retention.TotalHours);
        }
    }

    private async Task<int> DeleteOlderThanAsync(string collection, string dateField, Timestamp cutoff, CancellationToken ct)
    {
        var snap = await _db.Collection(collection)
            .WhereLessThan(dateField, cutoff)
            .Limit(500)
            .GetSnapshotAsync(ct);

        if (snap.Documents.Count == 0) return 0;

        var batch = _db.StartBatch();
        foreach (var doc in snap.Documents)
            batch.Delete(doc.Reference);

        await batch.CommitAsync(ct);
        return snap.Documents.Count;
    }
}
