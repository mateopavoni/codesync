# Convenciones

- **FACT** Local: `docker compose up --build` (web `127.0.0.1:4200`, API `127.0.0.1:5117`, emuladores Auth 9099 / Firestore 8082 / RTDB 9000, todo en `127.0.0.1`).
- **FACT** Tests backend unitarios: `dotnet test --filter "Category!=Integration"` (58). Integración (`Category=Integration`) necesita Java + firebase-tools; E2E Playwright necesita emuladores + API.
- **FACT** Proyecto de emuladores: `demo-codesync-test` (prefijo `demo-` = nunca toca un proyecto real).
- **FACT** CI: `.github/workflows/ci.yml` (tests unitarios de la API + build y tests del front).
- **FACT** Commits como `Mateo Pavoni <mateopavoni905@gmail.com>`, sin trailers de Claude. Nunca fetch/push a los remotes `dokku-api` / `dokku-web`.
- **FACT** Para entornos de poca RAM: correr cada suite por separado en contenedores con `--memory` y `-m:1` (dotnet).
