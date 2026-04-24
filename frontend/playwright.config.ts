import { defineConfig, devices } from '@playwright/test';

// Playwright spins up `vite preview` against the production build for E2E.
// The preview server needs a live backend — start it first with
// `docker compose up -d api` (or `dotnet run --project src/TaskManagement.Api`).
// See frontend/README.md for the full dance.

const env = process.env as Record<string, string | undefined>;
const baseURL = env['E2E_BASE_URL'] ?? 'http://localhost:5173';
const isCi = Boolean(env['CI']);
const webServer = env['E2E_NO_WEBSERVER']
  ? undefined
  : {
      command: 'pnpm preview',
      url: baseURL,
      reuseExistingServer: !isCi,
      stdout: 'pipe' as const,
      stderr: 'pipe' as const,
      timeout: 60_000,
    };

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: isCi,
  retries: isCi ? 2 : 0,
  workers: isCi ? 1 : undefined,
  reporter: isCi ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
    video: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  ...(webServer ? { webServer } : {}),
});
