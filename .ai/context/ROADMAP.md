# Roadmap (si se retoma)

1. Tests de integración (Firestore emulator) y Playwright en CI.
2. Probar el IA Coach con una key real de OpenRouter y validar/rotar el modelo `:free` por defecto.
3. Rate limiter compartido (Redis) para varias réplicas.
4. Tipos compartidos back↔front (NSwag).
5. Sandbox con VM/gVisor en vez de Docker plano, y sin montar el socket del host.
