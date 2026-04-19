import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Workload Heatmap', () => {
  test('heatmap page loads and shows heading', async ({ page }) => {
    await gotoPage(page, '/heatmap');
    await expect(page.getByRole('heading', { name: /Heatmap/ })).toBeVisible();
  });

  test('heatmap shows table after scheduler run', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');

    // Click refresh to load heatmap data
    const refreshBtn = page.getByTestId('heatmap-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    // Wait for the table to appear
    const table = page.getByTestId('heatmap-table');
    await expect(table).toBeVisible({ timeout: 15_000 });

    // Should have at least one row (one resource)
    const rows = table.locator('tbody tr');
    await expect(rows.first()).toBeVisible();
  });

  test('heatmap legend is visible', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');

    await page.getByTestId('heatmap-refresh').click();
    await expect(page.getByTestId('heatmap-table')).toBeVisible({ timeout: 15_000 });

    const legend = page.getByTestId('heatmap-legend');
    await expect(legend).toBeVisible();
  });
});
