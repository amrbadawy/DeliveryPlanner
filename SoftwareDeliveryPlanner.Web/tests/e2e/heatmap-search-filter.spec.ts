import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * heatmap-search-filter.spec.ts
 *
 * Search box in the heatmap filter sidebar narrows the visible resource rows.
 * Complements heatmap-filter.spec.ts which only covers role + clear; this
 * focuses purely on the search dimension.
 */

test.describe('Heatmap resource filter — search', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');
    const empty = page.getByTestId('heatmap-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }
    await expect(page.getByTestId('heatmap-table')).toBeVisible();
  });

  test('typing a substring of a resource name narrows the rows to matching resources only', async ({ page }) => {
    const table = page.getByTestId('heatmap-table');
    const rows = table.locator('tbody tr');
    const before = await rows.count();
    expect(before).toBeGreaterThan(0);

    // Pick a substring from the first row's resource name (always at least 3 chars).
    const firstName = ((await rows.first().locator('td.heatmap-resource-cell').textContent()) ?? '').trim();
    expect(firstName.length).toBeGreaterThan(2);
    const needle = firstName.slice(0, 3);

    const search = page.getByTestId('heatmap-filter-search');
    await search.fill(needle);

    // Filter is reactive (oninput); poll until row count stabilises below baseline,
    // and every visible row's resource label contains the needle (case-insensitive).
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBeLessThanOrEqual(before);

    const visible = await rows.count();
    expect(visible).toBeGreaterThan(0);
    for (let i = 0; i < visible; i++) {
      const label = ((await rows.nth(i).locator('td.heatmap-resource-cell').textContent()) ?? '').toLowerCase();
      expect(label).toContain(needle.toLowerCase());
    }
  });

  test('clearing the search restores the original row count', async ({ page }) => {
    const table = page.getByTestId('heatmap-table');
    const rows = table.locator('tbody tr');
    const before = await rows.count();

    const search = page.getByTestId('heatmap-filter-search');
    await search.fill('zzz_no_such_resource');
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBeLessThan(before);

    await search.fill('');
    await expect.poll(async () => rows.count(), { timeout: 5_000 }).toBe(before);
  });

  test('a no-match search empties the body but leaves the header intact', async ({ page }) => {
    const table = page.getByTestId('heatmap-table');
    const search = page.getByTestId('heatmap-filter-search');
    await search.fill('zzz_no_resource_matches_this');
    await expect.poll(async () => table.locator('tbody tr').count(), { timeout: 5_000 }).toBe(0);
    await expect(table.locator('thead tr')).toBeVisible();
  });
});
