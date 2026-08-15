/**
 * E2E: change password from /perfil (email/password users only).
 *
 * Verifies reauthenticateWithCredential + updatePassword actually take effect:
 * logs out after the change and logs back in with the NEW password.
 */
import { test, expect, Page } from '@playwright/test';

async function signUp(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/registro');
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(password);
  await page.locator('#confirmPassword').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/dashboard/, { timeout: 20_000 });
}

test('user changes password and can log in with the new one', async ({ page }) => {
  const ts = Date.now();
  const email = `e2e-changepw-${ts}@codesync.test`;
  const oldPassword = 'OldPass123!';
  const newPassword = 'NewPass456!';

  await signUp(page, email, oldPassword);

  await page.goto('/perfil');
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

  await page.locator('#currentPassword').fill(oldPassword);
  await page.locator('#newPassword').fill(newPassword);
  await page.locator('#confirmNewPassword').fill(newPassword);
  await page.getByRole('button', { name: 'Guardar contraseña' }).click();

  await expect(page.getByText('Contraseña actualizada.')).toBeVisible({ timeout: 15_000 });

  // Log out via the sidebar user menu, then log back in with the NEW password.
  await page.getByRole('button', { name: email }).click();
  await page.locator('.sidebar-logout').click();
  await page.waitForURL(/\/login/, { timeout: 15_000 });
  await page.locator('#email').fill(email);
  await page.locator('#password').fill(newPassword);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/\/dashboard/, { timeout: 20_000 });
});
