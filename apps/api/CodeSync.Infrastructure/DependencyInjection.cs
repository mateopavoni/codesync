using CodeSync.Application.Common.Interfaces;
using CodeSync.Infrastructure.AI;
using CodeSync.Infrastructure.Cleanup;
using CodeSync.Infrastructure.Execution;
using CodeSync.Infrastructure.Firebase;
using CodeSync.Infrastructure.Firestore.Repositories;
using CodeSync.Infrastructure.Firestore.Seeding;
using CodeSync.Infrastructure.RateLimit;
using Docker.DotNet;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Firebase Admin SDK ────────────────────────────────────────────────
        // When FIREBASE_AUTH_EMULATOR_HOST is set, the Admin SDK routes token
        // verification to the local Auth emulator and does not validate real credentials.
        // In that case we use a synthetic access token to avoid requiring a service account.
        var authEmulatorHost = Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");
        var isAuthEmulator = !string.IsNullOrEmpty(authEmulatorHost);

        if (FirebaseApp.DefaultInstance == null)
        {
            GoogleCredential credential = isAuthEmulator
                // Synthetic token — the Auth emulator does not validate Admin credentials.
                ? GoogleCredential.FromAccessToken("owner")
                : GoogleCredential.GetApplicationDefault();

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = configuration["Firebase:ProjectId"]
            });
        }

        // Token verifier (used by the API auth handler)
        services.AddScoped<IFirebaseTokenVerifier, FirebaseTokenVerifier>();

        // ── Firestore ─────────────────────────────────────────────────────────
        var projectId = configuration["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured.");

        // When FIRESTORE_EMULATOR_HOST is set, use EmulatorOnly to bypass OAuth.
        var firestoreEmulatorHost = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST");
        var firestoreDb = !string.IsNullOrEmpty(firestoreEmulatorHost)
            ? new FirestoreDbBuilder
              {
                  ProjectId = projectId,
                  EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
              }.Build()
            : FirestoreDb.Create(projectId);

        services.AddSingleton(firestoreDb);

        // Repositories
        services.AddScoped<IChallengeRepository, ChallengeFirestoreRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionFirestoreRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackFirestoreRepository>();
        services.AddScoped<IUserRepository, UserFirestoreRepository>();
        services.AddScoped<IRoomRepository, RoomFirestoreRepository>();

        // Seeder (dev only — called from Program.cs)
        services.AddTransient<ChallengeSeeder>();

        // ── Docker sandbox ────────────────────────────────────────────────────
        services.AddSingleton<DockerClient>(_ =>
        {
            var uri = configuration["Docker:EndpointUri"] ?? "npipe://./pipe/docker_engine";
            return new DockerClientConfiguration(new Uri(uri)).CreateClient();
        });
        services.AddSingleton<IDockerExecutor, DockerExecutor>();
        services.AddScoped<ICodeExecutionService, CodeExecutionService>();

        // ── AI Coach (Gemini) ─────────────────────────────────────────────────
        var geminiBase = configuration["Gemini:ApiBaseUrl"] ?? "https://generativelanguage.googleapis.com";
        services.AddHttpClient("Gemini", client =>
        {
            client.BaseAddress = new Uri(geminiBase);
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddScoped<GeminiApiClient>();
        services.AddScoped<IAICoachService, AICoachService>();

        // ── Rate limiter ──────────────────────────────────────────────────────
        // In-process singleton: one state per instance. Acceptable for MVP.
        services.AddSingleton<IRateLimiterService, InMemoryRateLimiter>();

        // ── Auto-borrado de datos demo (submissions/salas viejas) ──────────────
        services.AddHostedService<DemoDataCleanupService>();

        return services;
    }
}
