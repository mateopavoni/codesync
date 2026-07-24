// E2E environment — points Firebase client SDK to local emulators.
// Used via angular.json "e2e" build configuration.
// Requires Firebase emulators running:
//   npx firebase-tools emulators:start --only auth,firestore,database --project demo-codesync-test
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5117/api',
  useEmulators: true,
  emulatorAuthUrl: 'http://127.0.0.1:9099',
  firebase: {
    // Any non-empty API key works with emulators — they don't validate it.
    apiKey: 'fake-api-key-for-emulator',
    authDomain: 'demo-codesync-test.firebaseapp.com',
    // Realtime DB emulator URL
    databaseURL: 'http://127.0.0.1:9000?ns=demo-codesync-test',
    projectId: 'demo-codesync-test',
    storageBucket: 'demo-codesync-test.appspot.com',
    messagingSenderId: '000000000000',
    appId: '1:000000000000:web:0000000000000000',
  },
};
