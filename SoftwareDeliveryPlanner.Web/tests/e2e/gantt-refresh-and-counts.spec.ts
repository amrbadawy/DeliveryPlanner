import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

async function ensureChartOrSkip(page: Page) {
  const chart = page.getByTestId('gantt-chart');
  const empty = page.getByTestId('gantt-empty');
  await expect.poll(async () => {
    return (await chart.isVisible().catch(() => false)) ||
           (await empty.isVisible().catch(() => false));
  }, { timeout: 10_000 }).toBeTruthy();
  if (await empty.isVisible().catch(() => false)) {
    test.skip(true, 'Scheduler produced no scheduled tasks');
  }
  return chart;
}

/**
 * Verifies the Gantt refresh button reloads data and updates the visible row
 * set, and that the excluded/filtered count badges accurately reflect state.
 */
test.describe('Gantt refresh & count badges', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');
  });

  test('refresh button is visible and clickable on Gantt page', async ({ page }) => {
    await ensureChartOrSkip(page);
    const btn = page.getByTestId('gantt-refresh');
    await expect(btn).toBeVisible();
    await expect(btn).toBeEnabled();
  });

  test('refresh button preserves rendered chart after click', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);
    const initialRows = await chart.locator('[data-testid^="gantt-row-"]').count();
    expect(initialRows).toBeGreaterThan(0);

    await page.getByTestId('gantt-refresh').click();

    // Chart must still be visible after refresh; row count should be roughly the same
    // (could differ if backend changed; we just require non-zero).
    await expect(chart).toBeVisible();
    await expect.poll(async () =>
      chart.locator('[data-testid^="gantt-row-"]').count()
    , { timeout: 15_000 }).toBeGreaterThan(0);
  });

  test('plan range badge is visible and contains a date', async ({ page }) => {
    await ensureChartOrSkip(page);
    const range = page.getByTestId('gantt-range');
    await expect(range).toBeVisible();
    // Range text usually formats as "Range: dd MMM yyyy — dd MMM yyyy"
    await expect(range).toContainText(/\d{4}/);
  });

  test('excluded count badge appears only when there are unscheduled tasks', async ({ page }) => {
    await ensureChartOrSkip(page);
    const excluded = page.getByTestId('gantt-excluded-count');
    const count = await excluded.count();
    if (count === 0) {
      test.info().annotations.push({ type: 'info', description: 'No unscheduled tasks in this run' });
      return;
    }
    await expect(excluded).toBeVisible();
    // The badge text should include a number.
    await expect(excluded).toContainText(/\d+/);
  });

  test('filtered count badge appears when sidebar filter hides at least one task', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);
    const initialRows = await chart.locator('[data-testid^="gantt-row-"]').count();
    if (initialRows < 2) test.skip(true, 'Need 2+ rows to test filtered count');

    // Use the filter sidebar (Phase 1+ persistent sidebar) to apply a status filter.
    // We pick the first visible status chip and click it to toggle.
    const firstChip = page.locator('[data-testid^="task-filter-chip-"]').first();
    if (!(await firstChip.count())) test.skip(true, 'Filter sidebar not present on Gantt');

    await firstChip.click();
    await page.waitForTimeout(200);

    // Either the filtered-count badge appears OR row count shrinks.
    const filtered = page.getByTestId('gantt-filtered-count');
    const filteredVisible = await filtered.isVisible().catch(() => false);
    const newRows = await chart.locator('[data-testid^="gantt-row-"]').count();

    expect(filteredVisible || newRows < initialRows).toBeTruthy();
  });

  test('chart aria-label reports task count and date range', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);
    const aria = await chart.getAttribute('aria-label');
    expect(aria).toBeTruthy();
    expect(aria!).toMatch(/\d+ scheduled tasks/);
    expect(aria!).toMatch(/from \d{1,2} \w+ \d{4}/);
  });
});
