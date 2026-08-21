# Architecture — CodeSync

SaaS educativo: IDE colaborativo con editor en tiempo real, desafíos de código que se ejecutan en un sandbox aislado, IA Coach que da feedback automático, y salas de pair programming.

## Mapa del proyecto

```
codesync/
├── apps/
│   ├── api/                    .NET 8 — Clean Architecture + CQRS (MediatR)
│   │   ├── CodeSync.Api/       Controllers HTTP (ChallengeController, SubmissionController, CollaborationController, etc.)
│   │   ├── CodeSync.Application/  Handlers CQRS (GetChallengesHandler, CreateSubmissionHandler, JoinRoomHandler, etc.)
│   │   ├── CodeSync.Domain/    Entidades puras (Challenge, Submission, User, Feedback, Room) + value objects
│   │   ├── CodeSync.Infrastructure/  Servicios concretos (FirestoreRepository, DockerExecutor, GeminiApiClient, etc.)
│   │   └── CodeSync.Tests/     35 unit + integration tests (Firestore emulator real, no mocks)
│   │
│   └── web/                    Angular 20 standalone components
│       ├── src/app/
│       │   ├── core/           AuthService, FirebaseService, HttpClient interceptor
│       │   ├── editor/         MonacoEditorComponent, EditorPageComponent, ejecución
│       │   ├── collaboration/  CollaborationService, ChatComponent, room management
│       │   ├── dashboard/      DashboardComponent, progreso, feedback reciente
│       │   ├── auth/           LoginComponent, SignupComponent, authGuard
│       │   ├── shared/         Componentes reutilizables (buttons, cards, loading, etc.)
│       │   └── layouts/        AppShellComponent, topbar, sidebar
│       ├── styles.css          Design tokens (--cs-* custom properties, paleta MD3 dark)
│       └── environments/       Firebase config, API URLs
│
├── docs/
│   ├── design-tokens.md        Sistema de tokens: paleta, tipografía, escala, spacing
│   └── screenshots/            Capturas para README / ARCHITECTURE (a llenar con deploy)
│
├── infra/                      Docker Compose para ambiente local + deploy
├── .github/workflows/          CI/CD (tests, build)
└── LICENSE                     Propietario — all rights reserved
```

## Flujo de datos

```
[Angular Web]
    ↓ POST /api/submissions + Firebase Auth Token
    ↓
[.NET API]
    ├─→ FirebaseAuthenticationHandler valida el token JWT
    ├─→ SubmissionController → CreateSubmissionHandler
    ├─→ CodeExecutionService → DockerExecutor
    │   └─→ Corre código en contenedor con límites (256MB, 5s, sin red)
    ├─→ Si test falla → AICoachService → GeminiApiClient
    │   └─→ Rate limit 1/min + fallback a hints pre-generados
    └─→ Persiste resultado + feedback en Firestore
        ↓
[Firebase Realtime DB] ← sync de editor/cursores/chat (ambos sentidos)
    ↓
[Angular] recibe resultado, feedback, y actualiza UI
```

## Backend — Clean Architecture

5 proyectos, dependencias apuntando hacia adentro (sin ciclos):

### CodeSync.Api (punto de entrada HTTP)
- **Controllers:** `ChallengeController`, `SubmissionController`, `CollaborationController`, `AICoachController`, `UserController`, `DashboardController`
- **Auth:** `FirebaseAuthenticationHandler` (custom ASP.NET Core auth scheme) valida JWT con Firebase Admin SDK
- **Exception handler global:** mapea `KeyNotFoundException` → 404, `ValidationException` → 400, `InvalidOperationException` → 422, `UnauthorizedAccessException` → 401
- **Swagger:** configurado en `Program.cs`, servido en `/swagger` (dev) con documentación de endpoints

### CodeSync.Application (lógica de negocio, CQRS)
- **Queries:** `GetChallengesQuery`, `GetChallengeQuery`, `GetDashboardQuery`, `GetUserProfileQuery`
- **Commands:** `CreateChallengeCommand`, `CreateSubmissionCommand`, `CreateRoomCommand`, `JoinRoomCommand`, `UpsertUserProfileCommand`
- **Handlers:** implementan la lógica; usan repositorios e inyectados de `Infrastructure`
- **Validators:** `CreateSubmissionValidator`, `CreateRoomValidator` vía FluentValidation
- **Servicios de aplicación:** `CodeExecutionService`, `AICoachService` (orquestación, no raw infra)

### CodeSync.Domain (puro, sin dependencias)
- **Entidades:** `Challenge`, `Submission`, `User`, `Feedback`, `Room`
- **Value Objects:** `TestCase`, `ExecutionResult`, `RoomInviteCode`
- **Enums:** `DifficultyLevel` (Easy/Medium/Hard), `ProgrammingLanguage` (Python/JavaScript), `SubmissionStatus` (Pending/Running/Completed)
- **Repositorios (interfaces):** `IChallengRepository`, `ISubmissionRepository`, `IUserRepository`, `IFeedbackRepository`, `IRoomRepository`
- **Servicios (interfaces):** `ICodeExecutor`, `IAICoachClient`, `IFirebaseRealtime`

### CodeSync.Infrastructure (implementaciones concretas)
- **Firestore repositories:** `ChallengeFirestoreRepository`, `UserFirestoreRepository`, `SubmissionFirestoreRepository`, `FeedbackFirestoreRepository`, `RoomFirestoreRepository`
  - `RoomFirestoreRepository.JoinAsync` usa `FirestoreDb.RunTransactionAsync` para garantizar serialización en el cupo máximo de 4 usuarios (previene race condition last-write-wins)
- **Docker executor:** `DockerExecutor` vía `Docker.DotNet`; limites: `NetworkMode=none`, `MemorySwap=Memory` (256MB), `ReadonlyRootfs=true`, `User=nobody`, timeout 5s SIGKILL
- **Firebase Realtime:** `FirebaseRealtimeService` para sync de editor, cursores, chat
- **Gemini API Client:** `GeminiApiClient` HttpClient tipado (no SDK, solo REST) con fallback a `FallbackHintProvider`
- **Rate limiter:** `InMemoryRateLimiter` (diccionario con ventana configurable; no es persistente, reset en redeploy)
- **Seeding:** `ChallengeSeeder` carga 10 desafíos seed (Python + JavaScript) en desarrollo

### CodeSync.Tests (35 tests green)
- **Unit tests:** `CodeExecutionService` (6 tests: pass/partial-fail/timeout/syntax-error/docker-error/language-routing)
- **Unit tests CQRS:** `GetChallengeHandler` (found/not-found), `GetChallengesHandler` (order), `CreateRoomHandler` (happy path/challenge missing), `JoinRoomHandler` (add/idempotent/full/bad-code)
- **Unit tests utilidades:** `InMemoryRateLimiter` (5 tests: request/blocked/keys/expiry)
- **Integration tests:** 15 tests contra **Firestore emulator real** (no mocks)
  - `Challenge`, `User`, `Room` repository tests
  - **Prueba crítica:** `JoinRoomAsync` con 2 usuarios concurrentes por el último cupo; verifica que Firestore transaction serializa y solo uno gana (sin last-write-wins)
- **Tests de E2E:** pendiente (un agente de QA está terminando en paralelo; no tocar)

## Frontend — Angular 20 standalone

### Arquitectura por feature
Cada feature (auth, editor, dashboard, etc.) vive en su propia carpeta con componentes + servicios locales.

- **`core/`** — servicios singleton (Firebase, HTTP, Auth)
  - `AuthService`: Firebase Auth (Google, GitHub, Email)
  - `FirebaseService`: acceso a Realtime DB y Firestore
  - `HttpClient` interceptor automáticamente inyecta el Firebase ID Token en todos los requests
- **`auth/`** — `LoginComponent`, `SignupComponent`, `authGuard` (redirige a login si no autenticado)
- **`editor/`** — `EditorPageComponent` (contenedor principal de una sesión)
  - `MonacoEditorComponent` (wrapper): carga Monaco desde assets via AMD, integra con `CollaborationService`
  - `ExecutionPanelComponent` mostrá resultados + logs
  - `CoachPanelComponent` muestra feedback del IA Coach (o hints fallback)
- **`collaboration/`** — `CollaborationService` sincroniza editor/cursores/chat
  - `ChatComponent` en tiempo real
  - Cursores remotos renderizados como decoraciones Monaco
- **`dashboard/`** — progreso, desafíos completados, level, feedback reciente
- **`shared/`** — botones, cards, spinners, error message, etc. (sin lógica de negocio)
- **`layouts/`** — `AppShellComponent` (topbar fijo + sidebar con navegación)

### Design system
- **Tokens:** `docs/design-tokens.md` (paleta, tipografía, spacing, radios)
- **Paleta:** Material Design 3 dark → azul primario `#adc6ff`, verde secundario `#4edea3`, púrpura para IA Coach `#c89cff`
- **Tipografía:** Inter (body), JetBrains Mono (code)
- **CSS:** `styles.css` define `--cs-*` custom properties (reemplazó a variables sueltas)
- **Accesibilidad:** focus-visible global (ring 2px), `aria-live` en paneles dinámicos, `.sr-only` para screen readers, sin emojis decorativos

### Temas
Un solo tema: Material Design 3 dark (sin toggle light/dark — elegición de diseño para que sea opinado).

## Firebase

### Realtime Database (efímero, alta frecuencia)
```
/collaborations/{roomId}/
  code: string (última versión, último editador sincroniza cada 2s)
  cursors:
    {userId}: { line, column }
  chat[]:
    { userId, message, timestamp }
```

### Firestore (persistencia estructurada)
```
collections/
  challenges/       id: docId, title, description, difficulty, language, testCases[], seed
  submissions/      id, userId, challengeId, code, status, executionResult, error, createdAt
  feedback/         id, submissionId, userId, challengeId, coachFeedback, isFallback, createdAt
  users/            id (uid Firebase), displayName, avatar, level, completedChallenges[]
  rooms/            id, ownerId, memberIds[], inviteCode, createdAt
```

### Reglas de seguridad
```
Realtime:
  /collaborations/{roomId}/ → read/write si auth.uid ∈ room.memberIds
  
Firestore:
  submissions: create si auth.uid == userId (crear propia)
  submissions: read si auth.uid == userId (leer propia)
  feedback: read si auth.uid == userId (leer propia)
  users: write si auth.uid == uid (editar el propio perfil)
```

## Decisiones de arquitectura y trade-offs

### 1. Firestore como única persistencia estructurada (no SQL Server + Firestore)
**Por qué:** El MVP no necesita queries relacionales complejas. Firestore cubre desafíos, submissions, usuarios, feedback. Mantener una sola BD evita sincronización entre dos fuentes de verdad.

**Trade-off:** Si aparece un reporte complejo (ej. "desafíos más intentados por nivel"), se reconsideraría un data warehouse o replicación a BigQuery.

### 2. Gemini API para el IA Coach
**Por qué:** Costo y free tier. Mateo quiere un proyecto de portfolio defensible sin quemar presupuesto. Gemini tiene 60 requests/min gratis.

**Trade-off:** Gemini es menos potente en razonamiento complejo, pero para hints de programación básica (Python/JS nivel educativo) es más que suficiente.

### 3. Docker sandbox (no intérprete embebido ni remote execution)
**Por qué:** Seguridad. Ejecutar código arbitrario de usuarios en el backend sin aislamiento es suicidio. Docker no es perfecto (VM sería mejor), pero es el trade-off práctico: costo/complejidad vs. seguridad.

**Límites aplicados:**
- Sin red (`NetworkMode=none`) → no puede hacer requests exteriores
- 256MB RAM + sin swap (`MemorySwap=Memory`) → no puede consumir toda la máquina
- Timeout 5s SIGKILL → no puede colgar la API
- Filesystem read-only + `User=nobody` → no puede escribir ni escalar privilegios
- PidsLimit=50 → no puede fork bombas

**Riesgo residual:** Docker escape (parcheable con actualizaciones) o vulnerabilidades en el intérprete (Python/Node, out of scope).

### 4. Transacción de Firestore en el join a sala (sella el MVP)
**Problema real encontrado en tests de integración:** la primera versión de `JoinRoomAsync` era:
```csharp
room.MemberIds.Add(userId);
await db.Collection("rooms").Document(roomId).SetAsync(room, SetOptions.Overwrite);
```
Con 2 usuarios intentando unirse al mismo tiempo por el último cupo, ambos veían `Count < 4` y ambos se agregaban (last-write-wins, el segundo pisaba al primero pero creía que gané, el primero recibía 422 pero ya se había sumado). **Bug crítico de concurrencia.**

**Solución:** Envolver el read-check-write en `FirestoreDb.RunTransactionAsync`:
```csharp
await db.RunTransactionAsync(async txn => {
    var doc = await txn.GetAsync(roomRef);
    if (doc.Exists && doc.GetValue<Room>("memberIds").Count < 4) {
        // solo si pasa aquí, se ejecuta el write
        var updated = ...;
        txn.Update(roomRef, "memberIds", updated);
    } else {
        throw new InvalidOperationException("Sala llena o no existe");
    }
});
```
**Firestore serializa la transacción** → garantizado que solo uno gana, los demás abortan y reciben excepción → cliente reintenta o rechaza elegantemente. **Verificado con test de 2 joins concurrentes** en FASE 3. Ese test fue el que cazó el bug; es un talking point en entrevista (muestra debugging de race conditions reales).

### 5. Rate limiting en memoria (no Redis)
**Por qué:** MVP simple, una sola instancia. Si escala a múltiples servidores, Redis es obligatorio.

**Quirk:** reset en redeploy. Si Mateo restartea la API, los límites se resetean (no persisten). Para MVP está bien; en producción, usar Redis.

### 6. No hay paquete `shared/` entre backend y frontend
**Por qué:** Simplicidad. Mateo mantiene DTOs (.NET) y TypeScript interfaces (Angular) a mano. Si creciera, generaría TypeScript desde OpenAPI o usaría NSwag.

## Qué mejoraría con más tiempo

1. **Persistencia del rate limit** — migrar `InMemoryRateLimiter` a Redis para ambiente distribuido
2. **Cola de ejecuciones** — si el volumen de submissions sube, ejecutar código via BullMQ (o similar) en vez de sincrónico
3. **Tipos compartidos** — NSwag para generar TypeScript desde OpenAPI, evitar duplicación
4. **Observabilidad** — Application Insights + logs estructurados para producción
5. **Leaderboard + badges** — features del backlog; ya está la DB para soportarlas
6. **Modo profesor** — gestionar aulas, asignar desafíos, ver reportes por estudiante
7. **Más lenguajes** — Java, Go, Rust en el sandbox (hoy solo Python + JavaScript)
