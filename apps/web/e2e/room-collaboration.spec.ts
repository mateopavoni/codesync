/**
 * E2E: room creation + joining from a second browser context
 *
 * Simulates two independent users:
 *   - User 1: creates a room for a challenge
 *   - User 2: joins the room using the invite code returned by the API
 *
 * The invite code is captured via page.waitForResponse() on POST /api/rooms
 * because the UI redirects to the editor without displaying the code after creation.
 *
 * Real-time sync (cursors/chat) is powered by Firebase Realtime DB emulator (port 9000).
 */
import { test, expect, Browser, Page } from '@playwright/test';

// ─── Helpers ────────────────────────────────────────────────────────────────

async function signUp(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/registro');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(password);
  await page.locator('#confirmPassword').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/dashboard/, { timeout: 20_000 });
}

/**
 * Returns the first challenge ID from the /desafios list.
 * We need the Firestore document ID (in the card link href) to create a room.
 */
async function getFirstChallengeId(page: Page): Promise<string> {
  await page.goto('/desafios');
  await expect(page.locator('.challenge-card').first()).toBeVisible({ timeout: 15_000 });

  // Each card is an <a [routerLink]="['/desafios', challenge.id]">
  const href = await page.locator('.challenge-card').first().getAttribute('href');
  // href is like "/desafios/abc123" — extract the ID
  const parts = (href ?? '').split('/').filter(Boolean);
  const id = parts[parts.length - 1];
  if (!id) throw new Error('Could not extract challenge ID from card href: ' + href);
  return id;
}

// ─── Tests ──────────────────────────────────────────────────────────────────

test.describe('Room collaboration — 2 browser contexts', () => {

  test('user 1 creates room, user 2 joins with invite code', async ({ browser }: { browser: Browser }) => {
    const ts = Date.now();
    const email1 = `e2e-host-${ts}@codesync.test`;
    const email2 = `e2e-guest-${ts}@codesync.test`;
    const password = 'E2ePass123!';

    // ── User 1: create room ─────────────────────────────────────────────────
    const ctx1 = await browser.newContext();
    const page1 = await ctx1.newPage();

    await signUp(page1, email1, password);
    const challengeId = await getFirstChallengeId(page1);

    // Navigate to /colaboracion
    await page1.goto('/colaboracion');
    await expect(page1.locator('#challengeId')).toBeVisible({ timeout: 10_000 });

    // Intercept the POST /api/rooms response to capture inviteCode
    const [createResponse] = await Promise.all([
      page1.waitForResponse(
        (resp) => resp.url().includes('/api/rooms') && resp.request().method() === 'POST',
        { timeout: 15_000 },
      ),
      (async () => {
        await page1.locator('#challengeId').fill(challengeId);
        // The "Crear sala" button is the submit button inside the first form card
        await page1.locator('form').first().locator('button[type="submit"]').click();
      })(),
    ]);

    expect(createResponse.status()).toBe(201);
    const roomData = await createResponse.json();
    const inviteCode: string = roomData.inviteCode;
    expect(inviteCode).toBeTruthy();
    expect(inviteCode.length).toBe(6);

    // User 1 should be redirected to the editor in sala mode
    await page1.waitForURL(/\/editor\/.+\/sala\/.+/, { timeout: 15_000 });
    await expect(page1.locator('.room-indicator')).toBeVisible({ timeout: 10_000 });
    await expect(page1.locator('.room-indicator')).toContainText('Sala activa');

    // ── User 2: join room with invite code ──────────────────────────────────
    const ctx2 = await browser.newContext();
    const page2 = await ctx2.newPage();

    await signUp(page2, email2, password);

    await page2.goto('/colaboracion');
    await expect(page2.locator('#inviteCode')).toBeVisible({ timeout: 10_000 });

    // Intercept join response
    const [joinResponse] = await Promise.all([
      page2.waitForResponse(
        (resp) => resp.url().includes('/join') && resp.request().method() === 'POST',
        { timeout: 15_000 },
      ),
      (async () => {
        await page2.locator('#inviteCode').fill(inviteCode);
        await page2.locator('button.btn-secondary-action').click();
      })(),
    ]);

    expect(joinResponse.status()).toBe(200);

    // User 2 should also land in the editor for the same sala
    await page2.waitForURL(/\/editor\/.+\/sala\/.+/, { timeout: 15_000 });
    await expect(page2.locator('.room-indicator')).toBeVisible({ timeout: 10_000 });
    await expect(page2.locator('.room-indicator')).toContainText('Sala activa');

    // ── Verify both users are in the SAME room ──────────────────────────────
    const url1 = page1.url();
    const url2 = page2.url();
    // Both URLs should share the same roomId segment
    const roomIdFromUrl1 = url1.split('/sala/')[1];
    const roomIdFromUrl2 = url2.split('/sala/')[1];
    expect(roomIdFromUrl1).toBeTruthy();
    expect(roomIdFromUrl1).toBe(roomIdFromUrl2);

    // ── Cleanup ─────────────────────────────────────────────────────────────
    await ctx1.close();
    await ctx2.close();
  });

});
