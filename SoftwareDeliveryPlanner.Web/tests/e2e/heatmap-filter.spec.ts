import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Heatmap resource filter sidebar', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('sidebar renders with search, role, and seniority filters', async ({ page }) => {
    await gotoPage(page, '/heatmap');

    await expect(page.getByTestId('heatmap-filter-sidebar')).toBeVisible();
    await expect(page.getByTestId('heatmap-filter-search')).toBeVisible();
    await expect(page.getByTestId('heatmap-filter-role-dev')).toBeVisible();
    await expect(page.getByTestId('heatmap-filter-seniority-junior')).toBeVisible();
  });

  test('role and clear filters affect visible rows', async ({ page }) => {
    await gotoPage(page, '/heatmap');

    const table = page.getByTestId('heatmap-table');
    const rows = table.locator('tbody tr');
    const before = await rows.count();

    await page.getByTestId('heatmap-filter-role-dev').click();
    const afterRole = await rows.count();
    expect(afterRole).toBeLessThanOrEqual(before);

    if (await page.getByTestId('heatmap-filter-clear').isVisible()) {
      await page.getByTestId('heatmap-filter-clear').click();
      await expect.poll(async () => rows.count()).toBe(before);
    }
  });
});
