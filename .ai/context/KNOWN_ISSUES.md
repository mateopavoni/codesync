# Problemas conocidos

- **FACT** Rate limiter del IA Coach en memoria: una sola instancia, se resetea al reiniciar.
- **FACT** Cuota de OpenRouter `:free`: 50 requests/día sin créditos (FAQ de OpenRouter); la lista de modelos gratuitos cambia sin aviso.
- **FACT** Socket de Docker montado en la API (control del Docker del host).
- **FACT** Sin tipos compartidos back↔front (DTOs a mano). Go no está en el sandbox.
- **FACT** La web key de Firebase del proyecto original sigue en el **historial** de git (gitleaks: 2 hallazgos `gcp-api-key`); ya no está en el árbol. Es una key pública por diseño, pero conviene restringirla o borrar el proyecto.
- **FACT** Dependen de DNS/red: la primera `docker compose up` descarga imágenes base, jars de emuladores y las imágenes de runners.
