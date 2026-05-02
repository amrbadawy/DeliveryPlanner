import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const testDbPath = path.join(__dirname, '.playwright', 'planner-e2e.db');

fs.mkdirSync(path.dirname(testDbPath), { recursive: true });
process.env.PLANNER_DB_PATH = testDbPath;

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
    command: 'dotnet run --project "../SoftwareDeliveryPlanner.Web/SoftwareDeliveryPlanner.Web.csproj" --urls "http://localhost:2026"',
    url: 'http://localhost:2026',
    reuseExistingServer: false,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      PLANNER_DB_PATH: testDbPath,
      // Enables the env-gated test-fault seam (ITestFaultPolicy →
      // InMemoryTestFaultPolicy + POST /test-faults/{arm,clear}). Always 0
      // in production; only set here so e2e specs can deterministically
      // exercise failure-handling UI paths.
      SDP_TEST_FAULTS: '1',
    },
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
