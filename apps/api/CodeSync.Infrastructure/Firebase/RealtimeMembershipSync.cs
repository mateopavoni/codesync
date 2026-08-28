using System.Net.Http.Json;
using CodeSync.Application.Common.Interfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.Firebase;

internal sealed class RealtimeMembershipSync : IRealtimeMembershipSync
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email"
    ];

    private readonly HttpClient _http;
    private readonly ILogger<RealtimeMembershipSync> _logger;
    private readonly string? _emulatorHost;
    private readonly string? _databaseUrl;
    private readonly string? _projectId;
    private readonly GoogleCredential? _scopedCredential;

    public RealtimeMembershipSync(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RealtimeMembershipSync> logger)
    {
        _http = httpClientFactory.CreateClient("RealtimeDatabaseAdmin");
        _logger = logger;
        _projectId = configuration["Firebase:ProjectId"];

        // Mismo patrón que FIREBASE_AUTH_EMULATOR_HOST/FIRESTORE_EMULATOR_HOST en DependencyInjection.cs —
        // permite probar el scoping de membership local (firebase emulators:start) sin tocar el proyecto real.
        // El emulador de RTDB no valida OAuth de verdad: cualquier access_token (ej. "owner") pasa como admin.
        _emulatorHost = Environment.GetEnvironmentVariable("FIREBASE_DATABASE_EMULATOR_HOST");

        if (string.IsNullOrEmpty(_emulatorHost))
        {
            _databaseUrl = configuration["Firebase:DatabaseUrl"]?.TrimEnd('/');
            _scopedCredential = GoogleCredential.GetApplicationDefault().CreateScoped(Scopes);
        }
    }

    public async Task AddMemberAsync(string roomId, string userId, CancellationToken ct = default)
    {
        var url = await BuildUrlAsync($"collaborations/{roomId}/members/{userId}", ct);
        if (url is null) return;

        try
        {
            var response = await _http.PutAsJsonAsync(url, true, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "No se pudo espejar membership en RTDB para sala {RoomId}, usuario {UserId}: {Status}",
                    roomId, userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // No debe romper CreateRoom/JoinRoom — la colaboración en tiempo real quedaría sin scoping
            // hasta el próximo intento, pero el flujo principal (crear/unirse a la sala) sigue andando.
            _logger.LogWarning(ex, "Error espejando membership en RTDB para sala {RoomId}, usuario {UserId}", roomId, userId);
        }
    }

    public async Task RemoveRoomAsync(string roomId, CancellationToken ct = default)
    {
        var url = await BuildUrlAsync($"collaborations/{roomId}", ct);
        if (url is null) return;

        try
        {
            var response = await _http.DeleteAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("No se pudo borrar collaborations/{RoomId} en RTDB: {Status}", roomId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error borrando collaborations/{RoomId} en RTDB", roomId);
        }
    }

    /// <summary>Arma la URL REST de RTDB (real o emulador) con el token de acceso ya resuelto.
    /// Devuelve null si no hay Firebase:DatabaseUrl configurado (backend corriendo sin RTDB).</summary>
    private async Task<string?> BuildUrlAsync(string path, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_emulatorHost))
            return $"http://{_emulatorHost}/{path}.json?ns={_projectId}&access_token=owner";

        if (string.IsNullOrEmpty(_databaseUrl)) return null;

        var token = await _scopedCredential!.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        return $"{_databaseUrl}/{path}.json?access_token={token}";
    }
}
