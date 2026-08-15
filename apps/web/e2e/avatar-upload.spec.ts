/**
 * E2E: profile photo upload.
 *
 * Verifies the full path: file input -> Firebase Storage upload (emulator) ->
 * Firebase Auth profile update -> POST /api/users/me -> profile reload shows the new photo.
 *
 * Requires the Storage emulator too (port 9199) — see playwright.config.ts header.
 */
import { test, expect, Page } from '@playwright/test';
import path from 'path';

async function signUp(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/registro');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(password);
  await page.locator('#confirmPassword').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/dashboard/, { timeout: 20_000 });
}

test('user uploads a profile photo', async ({ page }) => {
  const ts = Date.now();
  await signUp(page, `e2e-avatar-${ts}@codesync.test`, 'E2ePass123!');

  await page.goto('/perfil');
  await expect(page.locator('.avatar-placeholder')).toBeVisible({ timeout: 15_000 });

  const fileInput = page.locator('.avatar-upload input[type="file"]');
  await fileInput.setInputFiles(path.join(__dirname, 'fixtures', 'test-avatar.png'));

  // changeAvatar() uploads to Storage, updates the Auth profile, then POSTs /api/users/me
  await page.waitForResponse(
    (resp) => resp.url().includes('/api/users/me') && resp.request().method() === 'POST',
    { timeout: 20_000 },
  );

  await expect(page.locator('.avatar')).toBeVisible({ timeout: 10_000 });
  await expect(page.locator('.avatar')).toHaveAttribute('src', /firebasestorage|127\.0\.0\.1|storage/);
  await expect(page.locator('.error-box')).toHaveCount(0);
});
