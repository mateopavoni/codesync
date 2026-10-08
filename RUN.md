# Cómo levantar CodeSync

Proyecto archivado: se corre **100% local**, con los emuladores de Firebase (no hace falta un
proyecto de Firebase ni cuenta de Google).

## Opción A — Docker Compose (recomendada)

Requisitos: Docker y ~2 GB de RAM libres.

```bash
# Una sola vez: imagen del runner de HTML/CSS (Chromium headless)
docker build -t codesync-html-runner:1.48.0 \
  -f apps/api/CodeSync.Infrastructure/Execution/docker/html-runner.Dockerfile \
  apps/api/CodeSync.Infrastructure/Execution/docker

docker compose up --build
```

- Web http://127.0.0.1:4200 · API http://127.0.0.1:5117/api
- Emuladores (solo `127.0.0.1`): Auth 9099, Firestore 8082, Realtime DB 9000. Datos en memoria.
- Los lenguajes Python/JS/Ruby/Java/C# usan imágenes públicas que el sandbox pullea solo.
- La API monta `/var/run/docker.sock` para lanzar los contenedores del sandbox: le da control del
  Docker del host, usalo solo en tu máquina.

### IA Coach (opcional)
Sin key usa hints pre-generados. Con una key gratuita de https://openrouter.ai/keys:

```bash
export OPENROUTER_API_KEY=sk-or-...
# opcional: export OPENROUTER_MODEL=google/gemma-4-31b-it:free
docker compose up --build
```

## Opción B — Desarrollo (sin Docker para la app)

Requisitos: .NET 8 SDK, Node 20+, Docker (sandbox) y Java 17 + `firebase-tools` (emuladores).

```bash
# 1. Emuladores
npx firebase-tools emulators:start --only auth,firestore,database --project demo-codesync-test

# 2. API apuntando a los emuladores
cd apps/api
export FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099
export FIRESTORE_EMULATOR_HOST=127.0.0.1:8082
export FIREBASE_DATABASE_EMULATOR_HOST=127.0.0.1:9000
export Firebase__ProjectId=demo-codesync-test
dotnet run --project CodeSync.Api

# 3. Frontend
cd apps/web && npm install && npm start
```

## Tests

```bash
# Backend, unitarios
cd apps/api && dotnet test --filter "Category!=Integration"

# Backend, integración (Firestore emulator real: necesita Java + firebase-tools)
cd apps/api && dotnet test --filter "Category=Integration"

# Frontend (unit)
cd apps/web && npm test

# E2E (Playwright): emuladores + API corriendo como en la opción B; Playwright levanta el front
cd apps/web && npx playwright test
```

El `webServer` de Playwright usa el puerto **4210** (no el 4200) y la config `e2e`, que apunta a
los emuladores. Si agregás un origin nuevo, agregalo también a `Cors:AllowedOrigins` en
`appsettings.json`.
