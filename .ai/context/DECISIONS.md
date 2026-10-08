# Decisiones

- **FACT** Firestore y no SQL; transacción para el cupo de sala (la versión optimista tenía una carrera detectada por un test de 2 joins concurrentes).
- **FACT** Docker y no VM para el sandbox: trade-off costo/complejidad vs. seguridad.
- **FACT** (2026-10-08) Gemini → OpenRouter: el modelo configurado era `gemini-1.5-flash` y OpenRouter ofrece modelos `:free` actuales con interfaz OpenAI. Default `google/gemma-4-31b-it:free` (multilingüe, 262k de contexto); reintento único con `openrouter/free` solo ante 404/5xx. Un 429 no se reintenta porque la cuota diaria de los `:free` es compartida.
- **FACT** (2026-10-08) El front compila contra emuladores por defecto; se quitó la config del proyecto Firebase real y GA4/GSC. Un e2e ya había contaminado producción una vez (ver `/ARCHITECTURE.md`).
- **FACT** El compose monta `/var/run/docker.sock` en la API: es lo que permite el sandbox, a costa de darle control del Docker del host.
