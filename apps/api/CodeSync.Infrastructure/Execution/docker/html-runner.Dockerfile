# Imagen para calificar desafíos de HTML (ver CodeExecutionService.BuildHtmlRunner).
#
# La imagen oficial mcr.microsoft.com/playwright:*-jammy trae Chromium/Firefox/WebKit
# y sus dependencias de SO preinstaladas, pero NO trae el paquete npm "playwright"
# (está pensada como base para que un proyecto haga su propio `npm ci`). Sin red
# dentro del container de ejecución (NetworkMode=none, no negociable — ver
# DockerExecutor), un `require('playwright')` a secas falla con "Cannot find
# module". Esta imagen resuelve eso instalando playwright-core (liviano, sin
# postinstall que vuelva a descargar navegadores — usa los que ya trae la base
# vía PLAYWRIGHT_BROWSERS_PATH) en el momento del build, que sí tiene red.
#
# Build local, una sola vez por host (no se publica a ningún registry todavía —
# mismo criterio que "necesitás Docker instalado" en RUN.md, no requiere CI/CD).
# Ver RUN.md para el comando exacto.
FROM mcr.microsoft.com/playwright:v1.48.0-jammy
RUN npm install -g playwright-core@1.48.0
ENV NODE_PATH=/usr/lib/node_modules
