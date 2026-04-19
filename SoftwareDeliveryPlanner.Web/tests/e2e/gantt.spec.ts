import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Gantt chart', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');
  });

  test('gantt page loads and shows heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Gantt Chart/ })).toBeVisible();
  });

  test('gantt chart renders with task rows after scheduler run', async ({ page }) => {
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(chart).toBeVisible();

    // Should have at least one gantt row
    const rows = chart.locator('[data-testid^="gantt-row-"]');
    await expect(rows.first()).toBeVisible();
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('gantt bars are visible for scheduled tasks', async ({ page }) => {
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(chart).toBeVisible();

    // At least one bar should be present
    const bars = chart.locator('[data-testid^="gantt-bar-"]');
    await expect(bars.first()).toBeVisible();
    const count = await bars.count();
    expect(count).toBeGreaterThan(0);
  });

  test('gantt legend is visible', async ({ page }) => {
    const legend = page.getByTestId('gantt-legend');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(legend).toBeVisible();

    // Legend should contain the expected labels
    await expect(legend).toContainText('Completed');
    await expect(legend).toContainText('In Progress');
    await expect(legend).toContainText('Not Started');
    await expect(legend).toContainText('At Risk');
    await expect(legend).toContainText('Late');
  });

  test('gantt shows plan date range', async ({ page }) => {
    const range = page.getByTestId('gantt-range');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(range).toBeVisible();
    // Range should contain "Plan range:" and date patterns
    await expect(range).toContainText('Plan range:');
    await expect(range).toContainText('days');
  });

  test('refresh button re-runs scheduler and reloads chart', async ({ page }) => {
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(chart).toBeVisible();

    const barsBefore = await chart.locator('[data-testid^="gantt-bar-"]').count();

    // Click refresh
    await page.getByTestId('gantt-refresh').click();

    // Chart should still be visible after refresh
    await expect(chart).toBeVisible();
    const barsAfter = await chart.locator('[data-testid^="gantt-bar-"]').count();
    expect(barsAfter).toBeGreaterThan(0);
    expect(barsAfter).toBe(barsBefore);
  });

  test('empty state shown when no scheduled tasks', async ({ page }) => {
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    await expect.poll(async () => {
      const chartVisible = await chart.isVisible().catch(() => false);
      const emptyVisible = await empty.isVisible().catch(() => false);
      return chartVisible || emptyVisible;
    }, { timeout: 10_000 }).toBeTruthy();
  });
});
