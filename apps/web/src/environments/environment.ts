export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  // Emulator support (off by default — activated in environment.e2e.ts)
  useEmulators: false,
  emulatorAuthUrl: '',
  firebase: {
    apiKey: 'REMOVED-FIREBASE-WEB-KEY',
    authDomain: 'codesync-95667.firebaseapp.com',
    databaseURL: 'https://codesync-95667-default-rtdb.firebaseio.com',
    projectId: 'codesync-95667',
    storageBucket: 'codesync-95667.firebasestorage.app',
    messagingSenderId: '235625621954',
    appId: '1:235625621954:web:be6a87d39c4fd46165ce5b',
  },
};
