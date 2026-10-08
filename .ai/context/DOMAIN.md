# Dominio

- **FACT** Lenguajes: Python, JavaScript, Ruby, Java, C#, HTML, CSS. HTML/CSS se califican con Chromium headless (imagen `codesync-html-runner:1.48.0`, se construye una vez).
- **FACT** Sala: máximo 4 usuarios, código de invitación de 6 caracteres sin ambiguos; el cupo se cierra con una transacción de Firestore (`RunTransactionAsync`), no con read-then-write.
- **FACT** Una entrega devuelve `allTestsPassed`, `testResults`, `aiFeedback` y `feedbackIsFallback`.
- **FACT** Un usuario nuevo no tiene perfil hasta `POST /api/users/me` (requiere `email`); antes `GET /api/users/me` da 404.
- **FACT** `DemoDataCleanupService` borra submissions/salas/feedback de más de 24 h cada hora.
