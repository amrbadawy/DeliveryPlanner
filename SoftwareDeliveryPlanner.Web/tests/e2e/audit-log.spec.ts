import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Activity / Audit Log', () => {
  test('audit log page loads', async ({ page }) => {
    await gotoPage(page, '/audit-log');
    await expect(page.getByRole('heading', { name: /Activity Log/ })).toBeVisible();
  });

  test('audit log shows feed or empty state after loading', async ({ page }) => {
    // Run scheduler to seed data and trigger domain events
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/audit-log');

    // Click refresh and wait for loading to complete
    const refreshBtn = page.getByTestId('audit-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    // Wait for loading state to complete — poll until feed or empty state appears
    const feed = page.getByTestId('audit-feed');
    const empty = page.getByTestId('audit-empty');

    await expect.poll(
      async () => (await feed.isVisible()) || (await empty.isVisible()),
      { timeout: 15_000 }
    ).toBeTruthy();
  });

  test('refresh button reloads audit entries', async ({ page }) => {
    await gotoPage(page, '/audit-log');

    const refreshBtn = page.getByTestId('audit-refresh');
    await expect(refreshBtn).toBeVisible();

    // Click refresh — should not error
    await refreshBtn.click();

    // Page should still show heading after refresh
    await expect(page.getByRole('heading', { name: /Activity Log/ })).toBeVisible();
  });
});
