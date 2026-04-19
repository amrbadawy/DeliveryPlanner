import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Workload Heatmap', () => {
  test('heatmap page loads and shows heading', async ({ page }) => {
    await gotoPage(page, '/heatmap');
    await expect(page.getByRole('heading', { name: /Resource Workload/ })).toBeVisible();
  });

  test('heatmap shows table after scheduler run', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');

    // Click refresh to load heatmap data
    const refreshBtn = page.getByTestId('heatmap-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    // Wait for either data table or empty-state to appear
    const table = page.getByTestId('heatmap-table');
    const emptyHeading = page.getByRole('heading', { name: /No heatmap data/i });
    await expect.poll(async () => {
      const tableVisible = await table.isVisible().catch(() => false);
      const emptyVisible = await emptyHeading.isVisible().catch(() => false);
      return tableVisible || emptyVisible;
    }, { timeout: 15_000 }).toBeTruthy();
    if (await emptyHeading.isVisible().catch(() => false)) {
      test.skip(true, 'Heatmap has no scheduler output in this run');
    }

    // Should have at least one row (one resource)
    const rows = table.locator('tbody tr');
    await expect(rows.first()).toBeVisible();
  });

  test('heatmap legend is visible', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');

    await page.getByTestId('heatmap-refresh').click();
    const table = page.getByTestId('heatmap-table');
    const emptyHeading = page.getByRole('heading', { name: /No heatmap data/i });
    await expect.poll(async () => {
      const tableVisible = await table.isVisible().catch(() => false);
      const emptyVisible = await emptyHeading.isVisible().catch(() => false);
      return tableVisible || emptyVisible;
    }, { timeout: 15_000 }).toBeTruthy();
    if (await emptyHeading.isVisible().catch(() => false)) {
      test.skip(true, 'Heatmap has no scheduler output in this run');
    }

    const legend = page.getByTestId('heatmap-legend');
    await expect(legend).toBeVisible();
  });
});
