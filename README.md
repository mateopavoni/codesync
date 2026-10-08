# CodeSync

> **IDE colaborativo para aprender a programar**, construido alrededor de un problema difícil:
> **el cupo de una sala de a lo sumo 4 personas nunca puede confiar en un read-then-write** — si
> dos usuarios piden el último lugar al mismo tiempo, un `Count < 4` leído por los dos deja entrar
> a los dos. El fix no es una nota en un README, es una transacción de Firestore verificada con un
> test de 2 joins concurrentes.

![estado](https://img.shields.io/badge/estado-archivado-lightgrey) ![stack](https://img.shields.io/badge/stack-.NET%208%20·%20Angular%2020%20·%20Firebase%20·%20Docker-2b2b2b) · ![license](https://img.shields.io/badge/license-proprietary-red)

Stack: **.NET 8 (Clean Architecture + CQRS) · Angular 20 standalone · Firestore + Realtime DB ·
Docker sandbox · OpenRouter**

---

## ¿Qué resuelve?

Un IDE colaborativo con desafíos de código tiene tres problemas que un CRUD no tiene:

1. **Concurrencia real en las salas** — el cupo máximo de 4 usuarios por sala se cierra con una
   transacción de Firestore (`RunTransactionAsync`), no con un `if (count < 4)` optimista. La
   primera versión sí era optimista: bajo 2 joins simultáneos por el último cupo, ambos leían
   `Count < 4` y ambos se agregaban (last-write-wins, el segundo pisaba al primero). El test de
   integración con 2 joins en paralelo contra el emulador de Firestore fue lo que lo detectó — ver
   [`ARCHITECTURE.md`](./ARCHITECTURE.md#4-transacción-de-firestore-en-el-join-a-sala-sella-el-mvp).
2. **Ejecutar código de usuarios no confiables, en un sandbox real** — 7 lenguajes (Python,
   JavaScript, Ruby, Java, C#, HTML, CSS) corren en contenedores Docker efímeros con
   `NetworkMode=none`, 256MB sin swap, timeout 5s con SIGKILL, filesystem read-only, `User=nobody`
   y `PidsLimit=50`. HTML/CSS se califica con Chromium headless real (Playwright) contra
   aserciones de DOM, no con un regex sobre el markup.
3. **Feedback que no se cae si el proveedor de IA falla** — el IA Coach (OpenRouter) tiene rate limit
   de 1 request/min por usuario y, si el proveedor no responde o no hay API key configurada, cae a hints
   pre-generados por dificultad. La corrida nunca se rompe por una IA caída.

### Features

- **Editor Monaco sincronizado en tiempo real** vía Firebase Realtime DB — código, cursores
  remotos y chat por sala, auto-save con debounce de 2s.
- **34 desafíos sembrados** en 7 lenguajes (Python, JavaScript, HTML, CSS, Ruby, Java, C#), con
  test cases visibles/ocultos y niveles de dificultad.
- **Ejecución sandboxed en Docker** por lenguaje, con límites de red/memoria/tiempo/privilegios
  (detalle completo en [Limitaciones conocidas](#limitaciones-conocidas) y `ARCHITECTURE.md`).
- **IA Coach (OpenRouter, modelos gratuitos)** con feedback en español, rate limit 1/min y fallback a hints pre-generados.
- **Salas colaborativas** — código de invitación de 6 caracteres sin ambiguos (`0`/`O`, `1`/`I`),
  cupo máximo de 4 serializado por transacción Firestore, chat y cursores en vivo.
- **Ranking global por nivel** — leaderboard ordenado por XP acumulado (`GetLeaderboardHandler`),
  agregado post-MVP a pedido.
- **Auth Firebase completo** — Google, GitHub y email/contraseña, cambio de contraseña, avatar
  con validación de extensión real (no solo content-type).
- **Auto-limpieza de datos de demo** — `DemoDataCleanupService` borra submissions/salas/feedback
  con más de 24h cada hora, para que un proyecto público no acumule basura indefinidamente.

### En números

**58 tests unitarios backend** (verificados en esta versión, corriendo con `dotnet test --filter
"Category!=Integration"`) + una suite de integración contra **Firestore emulator real** y **8 tests
E2E** de Playwright (signup → resolver desafío → sala colaborativa → cambio de contraseña →
avatar) que requieren los emuladores de Firebase · 5 controllers, 14 handlers CQRS (MediatR) · 7
lenguajes de ejecución sandboxed · arquitectura Api→Application→Domain (puro)←Infrastructure.

---

## Capturas

| Editor + ejecución | IA Coach | Sala colaborativa | Dashboard |
|---|---|---|---|
| ![Editor](./docs/screenshots/demo.png) | ![IA Coach](./docs/screenshots/coach.png) | ![Sala colaborativa](./docs/screenshots/room.png) | ![Dashboard](./docs/screenshots/dashboard.png) |

---

## Arquitectura (resumen)

El detalle —por qué Firestore y no SQL, por qué una API de LLM gratuita y no otro proveedor, por qué
`RunTransactionAsync` y no un lock optimista, qué se sacrificó a propósito— está en
[`ARCHITECTURE.md`](./ARCHITECTURE.md).

```mermaid
flowchart LR
    Client["Angular 20\nMonaco + Firebase SDK"] -->|"HTTP + Firebase ID Token"| Api["Controllers\nFirebaseAuthenticationHandler"]
    Api --> App["Application (CQRS/MediatR)\nCodeExecutionService · AICoachService"]
    App --> Docker["DockerExecutor\nsandbox efímero, sin red"]
    App --> LLM["OpenRouterApiClient\n+ FallbackHintProvider"]
    App --> Firestore[("Firestore\nchallenges·submissions·users·rooms·feedback")]
    Client <-->|"sync directo, 2s debounce"| RTDB[("Realtime DB\ncódigo·cursores·chat")]
    Cleanup["DemoDataCleanupService\n@1h, retención 24h"] -.-> Firestore
```

```
codesync/
├── apps/
│   ├── api/                    .NET 8 — Clean Architecture + CQRS (MediatR)
│   │   ├── CodeSync.Api/              Controllers HTTP + FirebaseAuthenticationHandler
│   │   ├── CodeSync.Application/      Handlers CQRS + CodeExecutionService + AICoachService
│   │   ├── CodeSync.Domain/           Entidades puras, sin dependencias externas
│   │   ├── CodeSync.Infrastructure/   FirestoreRepositories, DockerExecutor, OpenRouterApiClient
│   │   └── CodeSync.Tests/            tests (unit + integración contra Firestore emulator)
│   └── web/                    Angular 20 standalone
│       ├── src/app/            core, editor, collaboration, dashboard, auth, shared, layouts
│       └── e2e/                8 tests Playwright (auth, ejecución, salas, avatar, password)
├── docs/design-tokens.md       Sistema de diseño: paleta MD3 dark, tipografía, spacing
└── LICENSE                     Propietario — todos los derechos reservados
```

---

## Estado: archivado

El deploy público (Dokku en un VPS propio) y el proyecto Firebase original fueron dados de baja. El
repo se corre completo en local, **sin cuenta de Firebase**: la app usa los emuladores de Firebase
(Auth, Firestore, Realtime DB) dentro de Docker.

## Cómo correr

Requisitos: **Docker** (con ~2 GB de RAM libres). Nada más.

```bash
# 1. (una sola vez) imagen del runner de HTML/CSS (Chromium headless)
docker build -t codesync-html-runner:1.48.0 \
  -f apps/api/CodeSync.Infrastructure/Execution/docker/html-runner.Dockerfile \
  apps/api/CodeSync.Infrastructure/Execution/docker

# 2. todo el stack: emuladores de Firebase + API + web
docker compose up --build
```

- Web: http://127.0.0.1:4200 — API: http://127.0.0.1:5117/api — emuladores solo en `127.0.0.1`.
- Registrate con cualquier email/contraseña (el emulador de Auth no valida nada real). Los datos
  viven en memoria: se pierden al apagar el stack.
- **Ojo:** la API monta `/var/run/docker.sock` para crear los contenedores del sandbox. Eso le da
  control del Docker del host; usalo solo en tu máquina.

### IA Coach con OpenRouter (opcional)

Sin key, el Coach responde con hints pre-generados. Para feedback real con un modelo gratuito:

```bash
# key gratuita en https://openrouter.ai/keys
export OPENROUTER_API_KEY=sk-or-...
docker compose up --build
```

Modelo por defecto: `google/gemma-4-31b-it:free`; si OpenRouter lo retira o falla (404/5xx) se
reintenta una vez con `openrouter/free`. Los modelos `:free` tienen cuota diaria baja (50 requests
por día sin créditos, según el FAQ de OpenRouter) y la lista cambia seguido: se puede cambiar con
`OPENROUTER_MODEL`.

---

## Tests

```bash
# Backend — unitarios (58)
cd apps/api && dotnet test --filter "Category!=Integration"
# Backend — integración (necesita Java + firebase-tools para el emulador de Firestore)
cd apps/api && dotnet test --filter "Category=Integration"

# Frontend (unit)
cd apps/web && npm test -- --watch=false --browsers=ChromeHeadless

# E2E — 8 tests (necesita Firebase emulators + API apuntando a ellos, ver playwright.config.ts)
cd apps/web
npx firebase-tools emulators:start --only auth,firestore,database --project demo-codesync-test
# en otra terminal, con FIREBASE_AUTH_EMULATOR_HOST/FIRESTORE_EMULATOR_HOST seteados:
npx playwright test
```

La suite de integración corre contra **Firestore emulator real** (sin mocks de BD) y el sandbox de
ejecución corre **contenedores Docker reales** — más lento que mockear, pero es lo único que
prueba de verdad que el timeout, los límites de memoria y el aislamiento de red funcionan.

---

## Limitaciones conocidas

Ninguna de estas es un descuido — son simplificaciones deliberadas con un techo conocido:

- **Rate limiter del IA Coach en memoria** (`InMemoryRateLimiter`) — válido para una sola
  instancia; resetea en cada redeploy. Escalar a réplicas necesita Redis.
- **Sin deploy**: el proyecto está archivado; el CI (`.github/workflows/ci.yml`) corre los tests unitarios del backend y el build del front.
- **Docker, no una VM**, para el sandbox — trade-off consciente costo/complejidad vs. seguridad.
  Riesgo residual: Docker escape o vulnerabilidades del intérprete, ambos parcheables.
- **Sin tipos compartidos entre backend y frontend** — DTOs (.NET) y interfaces TypeScript se
  mantienen a mano; NSwag es la mejora natural si el proyecto crece.
- **Go no está en el sandbox** — evaluado y descartado por ahora: `go run` necesita un tmpfs
  ejecutable (incompatible con el `noexec /tmp` del contenedor) y compilar en frío toma ~4s, más
  de la mitad del timeout de ejecución por default.

---

## Licencia

© 2026 Mateo Pavoni. Todos los derechos reservados. Software propietario, publicado solo con fines
de evaluación/portfolio. Prohibida su copia, redistribución o reuso sin autorización escrita. Ver
[LICENSE](./LICENSE).
