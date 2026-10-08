# Arquitectura (resumen; detalle en /ARCHITECTURE.md)

- **FACT** `apps/api`: Api → Application → Domain (puro) ← Infrastructure. `apps/web`: Angular 20.
- **FACT** Auth: el front obtiene un ID token de Firebase Auth y lo manda como Bearer; la API lo valida (`FirebaseAuthenticationHandler`). Con `FIREBASE_AUTH_EMULATOR_HOST` el Admin SDK valida contra el emulador.
- **FACT** Datos: Firestore (challenges, submissions, users, rooms, feedback). Código/cursores/chat en vivo: Realtime DB, escrito directo por el cliente; la API espeja la membresía de sala en RTDB (`RealtimeMembershipSync`, soporta `FIREBASE_DATABASE_EMULATOR_HOST`).
- **FACT** Sandbox: `DockerExecutor` crea contenedores efímeros con `NetworkMode=none`, 256 MB sin swap, 1 CPU, `User=nobody`, `PidsLimit=50`, tmpfs `/tmp`. El código viaja por base64 en el comando (sin bind mounts), por eso funciona con el socket de Docker montado.
- **FACT** IA Coach: `AICoachService` → `OpenRouterApiClient` (REST compatible con OpenAI). Rate limit 1/min por usuario (`InMemoryRateLimiter`). Sin key, 429, truncado (`finish_reason=length`) o error → `FallbackHintProvider`.
- **FACT** El catálogo (`GET /api/challenges`) es público (`[AllowAnonymous]`); crear desafíos, entregas, salas y perfil exigen token.
