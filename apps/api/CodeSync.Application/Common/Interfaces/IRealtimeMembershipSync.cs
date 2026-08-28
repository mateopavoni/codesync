namespace CodeSync.Application.Common.Interfaces;

/// <summary>
/// Espeja membership de salas en Firebase Realtime Database (collaborations/{roomId}/members/{uid})
/// para que database.rules.json pueda scopear lectura/escritura por sala sin exponer todo el árbol
/// a cualquier usuario autenticado. Escribe vía la REST API de RTDB con un access token de Admin
/// (Application Default Credentials) — esas escrituras ignoran las reglas de seguridad, como
/// cualquier escritura del Admin SDK.
/// </summary>
public interface IRealtimeMembershipSync
{
    /// <summary>Agrega a userId como miembro de la sala en RTDB. No lanza si falla — logea y sigue
    /// (ver AICoachService/CodeExecutionService para el mismo patrón de "no romper el flujo principal
    /// por un servicio secundario"), así un fallo acá nunca tumba CreateRoom/JoinRoom.</summary>
    Task AddMemberAsync(string roomId, string userId, CancellationToken ct = default);

    /// <summary>Borra el subárbol completo collaborations/{roomId} (código, cursores, chat, members)
    /// cuando la sala se cierra, para no dejar membership stale que otorgue acceso a una sala muerta.</summary>
    Task RemoveRoomAsync(string roomId, CancellationToken ct = default);
}
