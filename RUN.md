# Cómo levantar CodeSync

## Requisitos
- .NET 8 SDK
- Node 20+ y Angular CLI (`npm i -g @angular/cli`)
- Docker (para el sandbox de ejecución de código)
  - Los lenguajes con función a testear (Python/JS/Ruby/Java/C#) usan imágenes públicas — se pullean solas.
  - HTML se califica en un sandbox con Chromium headless — requiere buildear una imagen local una sola vez:
    ```bash
    cd apps/api/CodeSync.Infrastructure/Execution/docker
    docker build -t codesync-html-runner:1.48.0 -f html-runner.Dockerfile .
    ```
- Proyecto de Firebase (Realtime DB + Firestore + Auth habilitados). La foto de perfil se guarda en disco del propio API, no en Firebase Storage (evita requerir plan Blaze).
- API key de Gemini (IA Coach)

## Variables de entorno
- Backend: copiá `apps/api/CodeSync.Api/appsettings.json` a `appsettings.Development.json` y completá `Firebase:ServiceAccountKeyPath`, `Gemini:ApiKey`.
- Frontend: completá `apps/web/src/environments/environment.ts` con la config de tu proyecto Firebase (apiKey, authDomain, databaseURL, projectId).

## Levantar

```bash
# Backend
cd apps/api
dotnet run --project CodeSync.Api

# Frontend (otra terminal)
cd apps/web
npm install
npm start
```

- Web: http://localhost:4200
- API: http://localhost:5117 (Swagger en `/swagger`)

## Tests

```bash
# Backend (necesita Docker corriendo + la imagen codesync-html-runner buildeada)
cd apps/api
dotnet test

# Frontend (unit)
cd apps/web
npm test
```

### E2E (Playwright)

Corre contra **Firebase emulators**, nunca contra el proyecto real — ver comentario al inicio de
`apps/web/playwright.config.ts`.

```bash
# 1. Emulators (otra terminal)
npx firebase-tools emulators:start --only auth,firestore,database --project demo-codesync-test

# 2. API apuntando a los emulators (otra terminal)
cd apps/api
$env:FIREBASE_AUTH_EMULATOR_HOST = "127.0.0.1:9099"
$env:FIRESTORE_EMULATOR_HOST     = "127.0.0.1:8082"
$env:Firebase__ProjectId         = "demo-codesync-test"
dotnet run --project CodeSync.Api

# 3. Playwright levanta el frontend solo (puerto 4210, configuración e2e)
cd apps/web
npx playwright test
```

El `webServer` de Playwright usa el puerto **4210**, no el 4200 de `npm start` — a propósito, para
que nunca pueda reusar por accidente un `ng serve` normal (que apunta a Firebase de producción).
Si agregás un origin nuevo acá, agregalo también a `Cors:AllowedOrigins` en `appsettings.json`.
