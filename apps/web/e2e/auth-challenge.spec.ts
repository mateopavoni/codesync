/**
 * E2E: auth → challenge → execution → IA Coach feedback
 *
 * Uses Firebase Auth emulator (port 9099) for signup/login.
 * Uses Firebase Firestore emulator (port 8082) via the .NET API.
 * Uses Docker (real containers) for sandboxed code execution.
 * IA Coach falls back to FallbackHintProvider because Gemini:ApiKey is empty — expected.
 */
import { test, expect, Page } from '@playwright/test';

// ─── Helpers ────────────────────────────────────────────────────────────────

const TEST_EMAIL    = `e2e-auth-${Date.now()}@codesync.test`;
const TEST_PASSWORD = 'E2ePass123!';

/**
 * Signs up a new user via the /registro form.
 * After signup the app redirects to /dashboard.
 */
async function signUp(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/registro');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(password);
  await page.locator('#confirmPassword').fill(password);
  await page.locator('button[type="submit"]').click();
  // The AuthService navigates to /dashboard after createUserWithEmailAndPassword
  await page.waitForURL(/\/dashboard/, { timeout: 20_000 });
}

/**
 * Sets the Monaco editor value.
 * Strategy 1: wait for window.monaco and use model.setValue() (cleanest).
 * Strategy 2 (fallback): wait for Monaco's internal textarea and type via keyboard.
 * Monaco AMD loading from assets can take 30-60s in headless Chromium.
 */
async function setEditorCode(page: Page, code: string): Promise<void> {
  // Wait for the loading spinner inside the editor wrapper to disappear,
  // which signals Monaco finished (or errored out).
  // The spinner is rendered by @if(loading()) — gone when loading() = false.
  await page.waitForFunction(
    () => {
      // The MonacoEditorComponent sets loading=false once initEditor() runs.
      // We can detect this by the absence of .btn-spinner inside .editor-wrapper
      // OR by checking window.monaco directly.
      const w = window as any;
      if (w.monaco && typeof w.monaco.editor?.getModels === 'function' && w.monaco.editor.getModels().length > 0) {
        return true;
      }
      return false;
    },
    // Use a very generous timeout — Monaco AMD loading from served assets takes time
    { timeout: 60_000, polling: 500 },
  );

  // Set value via Monaco model API (bypasses DOM entirely — most reliable)
  await page.evaluate((src: string) => {
    const w = window as any;
    const model = w.monaco?.editor?.getModels()?.[0];
    if (model) {
      model.setValue(src);
    }
  }, code);
}

// ─── Tests ──────────────────────────────────────────────────────────────────

test.describe('Auth + Challenge + Execution + IA Coach', () => {

  test('signup redirects to dashboard', async ({ page }) => {
    await signUp(page, TEST_EMAIL, TEST_PASSWORD);
    await expect(page).toHaveURL(/\/dashboard/);
    // Dashboard heading confirms the user is authenticated
    await expect(page.locator('h1, h2').filter({ hasText: /Dashboard|Bienvenido|Progreso/i })).toBeVisible();
  });

  test('challenge list shows challenges after login', async ({ page }) => {
    await signUp(page, `e2e-list-${Date.now()}@codesync.test`, TEST_PASSWORD);
    await page.goto('/desafios/lenguaje/python');
    // Wait for at least one challenge card to appear (seeder runs on API start)
    await expect(page.locator('.challenge-card').first()).toBeVisible({ timeout: 15_000 });
  });

  test('solve Python challenge with correct solution — all tests pass', async ({ page }) => {
    // Capture browser console errors to aid debugging
    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') consoleErrors.push(msg.text());
    });

    // 1. Auth
    await signUp(page, `e2e-solve-${Date.now()}@codesync.test`, TEST_PASSWORD);

    // 2. Navigate to challenge list
    await page.goto('/desafios/lenguaje/python');
    await expect(page.locator('.challenge-card').first()).toBeVisible({ timeout: 15_000 });

    // 3. Click by title, not position — the seed list has grown past the
    // original 10 challenges (now 34 across 7 languages), so card order is
    // no longer stable. Matching by title keeps this test correct regardless
    // of how many challenges get seeded later.
    await page.locator('.challenge-card').filter({ hasText: 'Encontrar el maximo' }).click();
    await page.waitForURL(/\/desafios\/.+/, { timeout: 10_000 });

    // 4. Click "Resolver desafío"
    await page.locator('a.btn-start').click();
    await page.waitForURL(/\/editor\/.+/, { timeout: 10_000 });

    // 5. Wait for Monaco editor container to be in the DOM
    await expect(page.locator('.editor-container')).toBeVisible({ timeout: 20_000 });

    // 6. Set a correct solution for "Encontrar el maximo" (Python)
    await setEditorCode(page, 'def solution(nums):\n    return max(nums)\n');

    // 7. Click "Ejecutar"
    await page.locator('button[aria-label="Ejecutar código"]').click();

    // 8. Wait for execution result — Docker container spin-up can take a few seconds
    await expect(page.locator('.summary .summary-text')).toBeVisible({ timeout: 30_000 });

    // 9. Assert all tests passed
    const summaryText = page.locator('.summary .summary-text');
    await expect(summaryText).toContainText('Todos los tests pasaron');

    if (consoleErrors.length > 0) {
      console.log('Browser console errors during test:', consoleErrors.join('\n'));
    }
  });

  test('wrong solution triggers IA Coach fallback feedback', async ({ page }) => {
    // 1. Auth
    await signUp(page, `e2e-coach-${Date.now()}@codesync.test`, TEST_PASSWORD);

    // 2. Navigate to the first Python challenge
    await page.goto('/desafios/lenguaje/python');
    await expect(page.locator('.challenge-card').first()).toBeVisible({ timeout: 15_000 });
    await page.locator('.challenge-card').first().click();
    await page.waitForURL(/\/desafios\/.+/, { timeout: 10_000 });
    await page.locator('a.btn-start').click();
    await page.waitForURL(/\/editor\/.+/, { timeout: 10_000 });

    // 3. Wait for editor container
    await expect(page.locator('.editor-container')).toBeVisible({ timeout: 20_000 });

    // 4. Submit a deliberately wrong solution (returns min instead of max)
    await setEditorCode(page, 'def solution(nums):\n    return min(nums)\n');

    // 5. Run it
    await page.locator('button[aria-label="Ejecutar código"]').click();

    // 6. Wait for execution to complete.
    // When the wrong solution fails AND fallback feedback is available, Angular
    // immediately switches activePanel to 'coach' (same tick as setting submissionResult).
    // This means .summary .summary-text is never visible in the DOM.
    // We wait for the coach panel instead, which is shown when activePanel === 'coach'.
    const coachTab = page.locator('.tab-btn').filter({ hasText: /IA Coach/i });
    await expect(coachTab).toBeVisible({ timeout: 30_000 });

    // 7. The coach tab should already be active (auto-switched on failure+feedback)
    // Ensure we're on the coach panel
    await expect(page.locator('.tab-btn.active').filter({ hasText: /IA Coach/i })).toBeVisible({ timeout: 5_000 });

    // 8. Feedback should be present (fallback hints since no Gemini API key configured)
    // The coach panel shows .feedback-message when feedback is a string
    await expect(page.locator('.feedback-message').first()).toBeVisible({ timeout: 10_000 });
  });

});
