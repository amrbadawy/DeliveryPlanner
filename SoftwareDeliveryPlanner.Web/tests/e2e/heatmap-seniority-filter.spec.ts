import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * heatmap-seniority-filter.spec.ts
 *
 * Seniority chips in the heatmap sidebar. heatmap-filter.spec.ts already covers
 * the role chip; this exercises the seniority dimension and verifies that
 * stacking chips within the same group is OR (count is monotonic non-decreasing
 * as more chips are added).
 */

test.describe('Heatmap resource filter — seniority', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');
    const empty = page.getByTestId('heatmap-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }
    await expect(page.getByTestId('heatmap-table')).toBeVisible();
  });

  test('selecting a single seniority chip narrows or holds the row count', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const before = await rows.count();

    await page.getByTestId('heatmap-filter-seniority-junior').click();
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBeLessThanOrEqual(before);
  });

  test('adding a second seniority chip never reduces the row count (OR within group)', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');

    await page.getByTestId('heatmap-filter-seniority-junior').click();
    const afterOne = await rows.count();

    await page.getByTestId('heatmap-filter-seniority-mid').click();
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBeGreaterThanOrEqual(afterOne);
  });

  test('toggling the same seniority twice returns to baseline', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const before = await rows.count();

    const chip = page.getByTestId('heatmap-filter-seniority-senior');
    await chip.click();
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBeLessThanOrEqual(before);
    await chip.click();
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBe(before);
  });

  test('chip aria-pressed reflects active state', async ({ page }) => {
    const chip = page.getByTestId('heatmap-filter-seniority-senior');
    await expect(chip).toHaveAttribute('aria-pressed', 'false');
    await chip.click();
    await expect(chip).toHaveAttribute('aria-pressed', 'true');
    await chip.click();
    await expect(chip).toHaveAttribute('aria-pressed', 'false');
  });
});
