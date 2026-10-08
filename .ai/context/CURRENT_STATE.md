# Estado actual (2026-10-08)

- **FACT** 53 tests unitarios backend pasan (contenedor `dotnet/sdk:8.0`, 1,5 GB, 1 CPU); incluye 8 de `OpenRouterApiClient`.
- **FACT** Imágenes del compose construyen (`firebase` 1,25 GB, `api` 338 MB, `web` 112 MB); el stack usa ~535 MB en reposo.
- **FACT** Smoke contra el stack real: signup en el emulador de Auth, 34 desafíos, detalle, entrega ejecutada en el sandbox Docker (falla con `name 'solution' is not defined`, esperado), perfil `POST/GET /me` 204/200, IA Coach con fallback.
- **FACT** OpenRouter verificado contra un servidor HTTP falso: ruta `/api/v1/chat/completions`, `Authorization: Bearer`, modelo por defecto, `feedbackIsFallback=false`; la 2ª entrega del mismo usuario cae al fallback (rate limit).
- **NO VERIFICADO** Llamada real a OpenRouter (no había API key), tests de integración (Firestore emulator), tests del front y Playwright e2e.
