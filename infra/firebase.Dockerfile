# Firebase emulators (Auth + Firestore + Realtime DB) for local runs.
# Build context: repo root. The emulator jars are downloaded at build time so
# `docker compose up` works without network access afterwards.
FROM node:20-bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends openjdk-17-jre-headless ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g firebase-tools@13.35.1 \
    && firebase setup:emulators:firestore \
    && firebase setup:emulators:database

WORKDIR /app
COPY firebase.compose.json database.rules.json firestore.indexes.json ./

# Keep the two JVM emulators small: this is a demo-sized dataset.
ENV JAVA_TOOL_OPTIONS="-Xmx256m"
EXPOSE 9099 8082 9000
CMD ["firebase", "emulators:start", "--config", "firebase.compose.json", "--only", "auth,firestore,database", "--project", "demo-codesync-test"]
