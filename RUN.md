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
- Proyecto de Firebase (Realtime DB + Firestore + Auth + Storage habilitados)
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
- API: http://localhost:5000 (Swagger en `/swagger` una vez agregado en FASE 5)

## Tests

```bash
# Backend
cd apps/api
dotnet test

# Frontend
cd apps/web
npm test
```
