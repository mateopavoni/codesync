export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  // Emulator support (off by default — activated in environment.e2e.ts)
  useEmulators: false,
  emulatorAuthUrl: '',
  firebase: {
    apiKey: 'PLACEHOLDER_API_KEY',
    authDomain: 'PLACEHOLDER_AUTH_DOMAIN',
    databaseURL: 'PLACEHOLDER_DATABASE_URL',
    projectId: 'PLACEHOLDER_PROJECT_ID',
    storageBucket: 'PLACEHOLDER_STORAGE_BUCKET',
    messagingSenderId: 'PLACEHOLDER_MESSAGING_SENDER_ID',
    appId: 'PLACEHOLDER_APP_ID',
  },
};
