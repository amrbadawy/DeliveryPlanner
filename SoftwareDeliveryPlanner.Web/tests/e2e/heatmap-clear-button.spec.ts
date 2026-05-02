import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * heatmap-clear-button.spec.ts
 *
 * The Clear button is a derived UI element: it must NOT be present on initial
 * load (no filter active) and must appear as soon as any filter dimension
 * (search, role, seniority) is engaged. Clicking it removes itself and
 * restores the unfiltered baseline.
 */

test.describe('Heatmap filter — Clear button', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');
    const empty = page.getByTestId('heatmap-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }
    await expect(page.getByTestId('heatmap-table')).toBeVisible();
  });

  test('Clear is not rendered when no filter is active', async ({ page }) => {
    await expect(page.getByTestId('heatmap-filter-clear')).toHaveCount(0);
  });

  test('Clear appears after a search term is entered, and disappears after click', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const before = await rows.count();

    await page.getByTestId('heatmap-filter-search').fill('zzz_no_match_xyz');
    await expect(page.getByTestId('heatmap-filter-clear')).toBeVisible();

    await page.getByTestId('heatmap-filter-clear').click();
    await expect(page.getByTestId('heatmap-filter-clear')).toHaveCount(0);
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBe(before);
    await expect(page.getByTestId('heatmap-filter-search')).toHaveValue('');
  });

  test('Clear appears after a role chip is selected and clears it on click', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const before = await rows.count();

    const devChip = page.getByTestId('heatmap-filter-role-dev');
    await devChip.click();
    await expect(devChip).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('heatmap-filter-clear')).toBeVisible();

    await page.getByTestId('heatmap-filter-clear').click();
    await expect(devChip).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('heatmap-filter-clear')).toHaveCount(0);
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBe(before);
  });

  test('Clear resets all three dimensions in a single click', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const before = await rows.count();

    await page.getByTestId('heatmap-filter-search').fill('a');
    await page.getByTestId('heatmap-filter-role-dev').click();
    await page.getByTestId('heatmap-filter-seniority-senior').click();
    await expect(page.getByTestId('heatmap-filter-clear')).toBeVisible();

    await page.getByTestId('heatmap-filter-clear').click();

    await expect(page.getByTestId('heatmap-filter-search')).toHaveValue('');
    await expect(page.getByTestId('heatmap-filter-role-dev')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('heatmap-filter-seniority-senior')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('heatmap-filter-clear')).toHaveCount(0);
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBe(before);
  });
});
