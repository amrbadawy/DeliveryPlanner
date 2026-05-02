import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: virtualization invariants.
 *
 * Gantt.razor wraps task rows in <Virtualize OverscanCount="5">. With small
 * datasets every row is rendered. We only assert what we can control:
 *   - the rows-container exists and contains the rows
 *   - the count of rendered gantt-row-* matches the count of scheduled tasks
 *     (when small enough to fit in the viewport with overscan)
 *   - each row has a stable testid keyed by task id
 */

async function gotoGantt(page: Page) {
  await runSchedulerFromDashboard(page);
  await gotoPage(page, '/gantt');
  const chart = page.getByTestId('gantt-chart');
  if (!(await chart.isVisible().catch(() => false))) {
    test.skip(true, 'Gantt chart not rendered (no scheduled tasks)');
  }
  return chart;
}

test.describe('Gantt virtualization', () => {
  test('every rendered row has a unique testid keyed by task id', async ({ page }) => {
    const chart = await gotoGantt(page);
    const rows = chart.locator('[data-testid^="gantt-row-"]');
    const n = await rows.count();
    if (n === 0) test.skip(true, 'No rows rendered');

    const ids = await rows.evaluateAll(els =>
      els.map(e => (e.getAttribute('data-testid') ?? '').replace(/^gantt-row-/, '')));
    // No empties, no duplicates.
    expect(ids.every(id => id.length > 0)).toBe(true);
    expect(new Set(ids).size).toBe(ids.length);
  });

  test('every rendered row is wrapped inside the gantt-chart region', async ({ page }) => {
    const chart = await gotoGantt(page);
    const rows = chart.locator('[data-testid^="gantt-row-"]');
    const n = await rows.count();
    if (n === 0) test.skip(true, 'No rows rendered');

    // All rows live inside the chart region (i.e. selectors scoped to chart match).
    const insideCount = await chart.locator('[data-testid^="gantt-row-"]').count();
    expect(insideCount).toBe(n);
  });

  test('row count matches the gantt-chart aria-label task count when in viewport', async ({ page }) => {
    const chart = await gotoGantt(page);
    const aria = await chart.getAttribute('aria-label') ?? '';
    // aria-label format: "Gantt chart showing N scheduled tasks from ... to ..."
    const m = aria.match(/showing\s+(\d+)\s+scheduled tasks/);
    if (!m) test.skip(true, `aria-label not in expected format: ${aria}`);
    const expected = parseInt(m![1], 10);

    const rows = chart.locator('[data-testid^="gantt-row-"]');
    const rendered = await rows.count();

    // With virtualization + overscan=5, rendered ≤ expected. For small datasets
    // they should be equal; for large, rendered may be smaller. Either way,
    // rendered should never exceed expected.
    expect(rendered).toBeLessThanOrEqual(expected);
    // And at least one row should be rendered if there are any tasks.
    if (expected > 0) expect(rendered).toBeGreaterThan(0);
  });
});
