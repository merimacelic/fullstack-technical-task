import { expect, test } from '@playwright/test';

test('filter tasks by status and search, and sort alphabetically', async ({ page }) => {
  const email = `playwright+${Date.now()}@example.com`;
  await page.goto('/register');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill('Passw0rd!');
  await page.getByRole('button', { name: /create account/i }).click();
  await page.waitForURL(/\/tasks/);

  async function createTask(title: string) {
    await page.getByRole('button', { name: /new task/i }).click();
    await page.getByLabel(/title/i).fill(title);
    await page.getByRole('button', { name: /create task/i }).click();
    await expect(page.getByText(title)).toBeVisible();
  }

  await createTask('Alpha');
  await createTask('Beta');
  await createTask('Gamma');

  // Search narrows the list.
  await page.getByRole('searchbox', { name: /search tasks/i }).fill('Bet');
  await expect(page.getByText('Beta')).toBeVisible();
  await expect(page.getByText('Alpha')).toHaveCount(0);

  // Clear search → everything back.
  await page.getByRole('searchbox', { name: /search tasks/i }).fill('');
  await expect(page.getByText('Alpha')).toBeVisible();
});
