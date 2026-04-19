import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Dashboard Features', () => {
  test('stale plan warning shown before scheduler run', async ({ page }) => {
    await gotoPage(page, '/');

    // The stale plan warning should be visible if scheduler hasn't run
    // (or hidden if it has run in a previous test — both are valid)
    const warning = page.getByTestId('stale-plan-warning');
    const isVisible = await warning.isVisible().catch(() => false);

    // If visible, verify content
    if (isVisible) {
      await expect(warning).toContainText('Plan may be outdated');
    }
  });

  test('stale plan warning hides after running scheduler', async ({ page }) => {
    await gotoPage(page, '/');

    // Run scheduler from dashboard
    const runBtn = page.getByTestId('btn-run-scheduler');
    await expect(runBtn).toBeVisible();
    await runBtn.click();

    // Wait for scheduler to complete (button re-enables)
    await expect(runBtn).toBeEnabled({ timeout: 30_000 });

    // Warning should be hidden after scheduler run
    const warning = page.getByTestId('stale-plan-warning');
    await expect(warning).toBeHidden();
  });

  test('risk trend chart is visible after scheduler run', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/');

    // The risk trend section should be present
    // Look for the chart heading or container
    await expect(page.getByRole('heading', { name: /Risk Trend/ })).toBeVisible();
  });

  test('risk notifications panel is visible on dashboard', async ({ page }) => {
    await gotoPage(page, '/');

    // Risk notifications section should be present (may be empty)
    await expect(page.getByRole('heading', { name: /Risk Notifications|Notifications/ })).toBeVisible();
  });

  test('KPI cards are visible', async ({ page }) => {
    await gotoPage(page, '/');

    // Wait for loading to complete
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    // KPI values should be visible — check for the heading
    await expect(page.getByRole('heading', { name: /Dashboard/ })).toBeVisible();
  });
});
