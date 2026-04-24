import { expect, test } from '@playwright/test';

test('create, complete, and delete a task', async ({ page }) => {
  const email = `playwright+${Date.now()}@example.com`;
  const taskTitle = `E2E Task ${Date.now()}`;

  await page.goto('/register');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill('Passw0rd!');
  await page.getByRole('button', { name: /create account/i }).click();
  await page.waitForURL(/\/tasks/);

  // Create
  await page.getByRole('button', { name: /new task/i }).click();
  await page.getByLabel(/title/i).fill(taskTitle);
  await page.getByRole('combobox', { name: /priority/i }).click();
  await page.getByRole('option', { name: /high/i }).click();
  await page.getByRole('button', { name: /create task/i }).click();

  await expect(page.getByText(taskTitle)).toBeVisible();

  // Complete
  await page.getByRole('button', { name: new RegExp(`mark ${taskTitle} as complete`, 'i') }).click();
  await expect(page.getByRole('button', { name: new RegExp(`reopen task ${taskTitle}`, 'i') })).toBeVisible();

  // Delete via card menu → confirm
  await page.getByRole('button', { name: new RegExp(`actions menu for ${taskTitle}`, 'i') }).click();
  await page.getByRole('menuitem', { name: /delete/i }).click();
  await page.getByRole('button', { name: /^delete$/i }).click();

  await expect(page.getByText(taskTitle)).toHaveCount(0);
});
