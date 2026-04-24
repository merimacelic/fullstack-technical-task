import { expect, test } from '@playwright/test';

// Smoke: register → see empty task list → sign out → re-land on login.
// A unique email per run keeps the suite repeatable without an explicit DB reset.
test('register, sign out, and return to login', async ({ page }) => {
  const email = `playwright+${Date.now()}@example.com`;

  await page.goto('/register');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill('Passw0rd!');
  await page.getByRole('button', { name: /create account/i }).click();

  await expect(page).toHaveURL(/\/tasks/);
  await expect(page.getByRole('heading', { name: /tasks/i })).toBeVisible();

  await page.getByRole('button', { name: /open account menu/i }).click();
  await page.getByRole('menuitem', { name: /sign out/i }).click();

  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
});
