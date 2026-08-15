import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for CodeSync.
 *
 * Pre-requisites before running `npx playwright test`:
 *  1. Firebase emulators running (auth:9099, firestore:8082, database:9000, storage:9199):
 *       cd <project-root>
 *       npx firebase-tools emulators:start --only auth,firestore,database,storage --project demo-codesync-test
 *
 *  2. .NET API running with emulator env vars:
 *       $env:FIREBASE_AUTH_EMULATOR_HOST = "127.0.0.1:9099"
 *       $env:FIRESTORE_EMULATOR_HOST     = "127.0.0.1:8082"
 *       $env:Firebase__ProjectId         = "demo-codesync-test"
 *       dotnet run --project apps/api/CodeSync.Api
 *
 *  3. Angular dev server (started automatically by `webServer` below with e2e config).
 *
 * The `webServer` block starts `ng serve --configuration=e2e` if no server is already
 * listening on port 4200 (`reuseExistingServer: true`).
 */
export default defineConfig({
  testDir: './e2e',

  // Maximum time per test (Monaco AMD loading + Docker container spin-up can be slow)
  timeout: 120_000,

  // One retry on CI, none locally — avoids hiding flaky infrastructure
  retries: 0,

  // Sequential: avoids two tests competing for the same emulator state
  workers: 1,

  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],

  use: {
    baseURL: 'http://localhost:4200',
    // Capture traces only on first retry — useful for debugging
    trace: 'on-first-retry',
    // Headless for speed; set to false to watch the browser during development
    headless: true,
    // Generous timeouts: Angular lazy loading + Monaco AMD init from assets
    actionTimeout: 60_000,
    navigationTimeout: 30_000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Start the Angular dev server in e2e mode automatically.
  // The emulators and .NET API must be started manually beforehand.
  webServer: {
    command: 'npx ng serve --configuration=e2e --port 4200',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
