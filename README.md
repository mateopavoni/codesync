<div align="center">
  <h1>CodeSync</h1>
  <p>IDE colaborativo para aprender a programar: desafíos con ejecución sandboxed, IA Coach que da feedback automático, y pair programming en vivo.</p>

  ![Build](https://img.shields.io/badge/build-passing-brightgreen) ![License](https://img.shields.io/badge/license-proprietary-blue)

  ![CodeSync demo — editor en tiempo real y ejecución de desafío](docs/screenshots/demo.png)
  *(Captura pendiente: mostrará el editor Monaco con código de ejemplo, resultados de test y feedback del IA Coach)*

  <a href="#quickstart">Quickstart</a> · <a href="ARCHITECTURE.md">Architecture</a> · <a href="#api">API Docs</a> · <a href="#demo">Demo</a>
</div>

## El problema

Aprender a programar en solitario es lento: no hay feedback inmediato cuando cometes un error, y el pair programming real requiere cambiar entre varias herramientas (editor, chat, terminal). CodeSync unifica las tres cosas: un editor colaborativo con ejecución sandboxed en el backend y un IA Coach que da hints cuando falla un test.

## Stack

| Capa | Tecnología | Por qué |
|---|---|---|
| **Backend** | .NET 8, Clean Architecture + CQRS (MediatR) | separación clara de responsabilidades, fácil de testear y mantener |
| **Frontend** | Angular 20, standalone components | reactividad con RxJS, tipado fuerte end-to-end |
| **Editor** | Monaco Editor (embebido) | soporte de sintaxis + temas oscuros, usado en VS Code |
| **Sync en tiempo real** | Firebase Realtime Database | sincroniza código/cursores/chat sin que tengas que gestionar WebSockets |
| **Persistencia** | Firestore | desafíos, submissions, feedback, perfil de usuario con transacciones ACID |
| **Ejecución de código** | Docker (sandbox aislado) | ejecuta código arbitrario de usuarios no confiables con límites de memoria, tiempo, y sin acceso a red |
| **IA Coach** | Gemini API | feedback tipo mentor (hints, no soluciones) con rate limiting |

## Features

- **Editor colaborativo en tiempo real** — múltiples usuarios ven el código sincronizado, cursores remotos visibles, sin lag
- **Desafíos de código** — 10 desafíos seed (Python + JavaScript): suma, reversa de string, máximo, FizzBuzz, Fibonacci, palíndromo
- **Ejecución sandboxed** — código se corre en un contenedor Docker aislado: sin red, máx 256MB RAM, timeout 5s, usuario sin privilegios
- **IA Coach** — cuando falla un test, un prompt automático a Gemini genera feedback contextualizado (nunca resuelve el ejercicio). Rate-limited a 1/min por usuario + fallback a hints pre-generados
- **Salas colaborativas** — máx 4 usuarios por sala, código compartido, chat en vivo, join mediante código de invitación
- **Dashboard de progreso** — desafíos completados, nivel, feedback reciente del coach
- **Autenticación** — Google, GitHub, Email vía Firebase Auth

## Quickstart

Ver [RUN.md](RUN.md) para requisitos completos, variables de entorno, y pasos de setup de Firebase.

```bash
# Backend (.NET 8)
cd apps/api
dotnet run --project CodeSync.Api

# Frontend (Angular 20, otra terminal)
cd apps/web
npm install
npm start
```

Luego abrí http://localhost:4200 en tu navegador.

## API

El backend expone una API REST documentada con OpenAPI (Swagger):

```
GET  /api/challenges              Listar desafíos
GET  /api/challenges/{id}         Detalles de un desafío
POST /api/submissions             Ejecutar código y obtener resultado
GET  /api/rooms                   Listar salas del usuario
POST /api/rooms                   Crear una sala nueva
POST /api/rooms/{id}/join         Unirse a una sala
GET  /api/dashboard               Progreso del usuario + feedback reciente
POST /api/users/me                Upsert perfil
GET  /api/users/me                Obtener perfil actual
```

Vé a `http://localhost:5000/swagger` (env dev) o `/api/docs` (producción) para explorar interactivamente.
**Autenticación:** todos los endpoints (excepto login/signup) requieren un Firebase ID Token en el header `Authorization: Bearer <token>`.

## Demo

Pendiente de deployment. Una vez en vivo, irá acá el link a la instancia deployada.

## Architecture

Ver [ARCHITECTURE.md](ARCHITECTURE.md) para el mapa completo: decisiones de diseño, flujo de datos, trade-offs y qué mejoraría con más tiempo.

## Licencia

Todos los derechos reservados. Proyecto personal de Mateo Pavoni.
Ver [LICENSE](LICENSE) para los términos completos.

## Changelog

| Versión | Fecha | Cambio |
|---------|-------|--------|
| v0.1.0  | 2026-08-05 | MVP: auth, editor colaborativo, 10 desafíos, sandbox Docker, IA Coach, salas, dashboard. 35/35 tests pasando. Pulido UX con Stitch AI. |
