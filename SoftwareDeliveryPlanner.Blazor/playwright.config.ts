import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';

const testDbPath = path.join(__dirname, '.playwright', 'planner-e2e.db');

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:2026',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    viewport: { width: 1440, height: 900 },
  },
  webServer: {
    command: 'dotnet run --project "D:\\OpenCode\\SoftwareDeliveryPlanner.Blazor\\SoftwareDeliveryPlanner.Blazor.csproj" --urls "http://localhost:2026"',
    url: 'http://localhost:2026',
    reuseExistingServer: true,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      PLANNER_DB_PATH: testDbPath,
    },
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
