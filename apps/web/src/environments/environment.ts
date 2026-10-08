// Local-only config: the Firebase client SDK talks to the emulators started by
// `docker compose up` (see /docker-compose.yml). The original production Firebase
// project was retired; no real project credentials live in this repo.
export const environment = {
  production: false,
  apiUrl: 'http://127.0.0.1:5117/api',
  useEmulators: true,
  emulatorAuthUrl: 'http://127.0.0.1:9099',
  firebase: {
    // Any non-empty API key works with the emulators — they don't validate it.
    apiKey: 'fake-api-key-for-emulator',
    authDomain: 'demo-codesync-test.firebaseapp.com',
    // Realtime DB emulator (the host/port are wired in app.config.ts)
    databaseURL: 'http://127.0.0.1:9000?ns=demo-codesync-test',
    projectId: 'demo-codesync-test',
    storageBucket: 'demo-codesync-test.appspot.com',
    messagingSenderId: '000000000000',
    appId: '1:000000000000:web:0000000000000000',
  },
};
