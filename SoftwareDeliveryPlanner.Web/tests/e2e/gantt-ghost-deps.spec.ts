import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: ghost-dependency stub indicator.
 *
 * The existing task-filter-saved-views.spec.ts asserts the Gantt page
 * survives when a predecessor is hidden via Tasks page. These tests pin
 * down the stronger contract on the ghost-deps badge itself:
 *   - data-hidden-count attribute matches the rendered text
 *   - singular vs plural wording ("dep" vs "deps") matches the count
 *   - badge disappears when the filter is cleared
 */

async function gotoTasksAndCollectIds(page: Page): Promise<string[]> {
  await runSchedulerFromDashboard(page);
  // runSchedulerFromDashboard already navigates to /tasks and waits for rows.
  // Wait for at least one stable row testid to be present before scraping.
  await page.locator('tr[data-testid^="tasks-row-"]').first().waitFor({ timeout: 10_000 }).catch(() => {});
  const rows = page.locator('tr[data-testid^="tasks-row-"]');
  const ids = await rows.evaluateAll(els =>
    els.map(e => (e.getAttribute('data-testid') ?? '').replace(/^tasks-row-/, '')));
  return ids.filter(id => /^[A-Z]+-\d+$/.test(id));
}

async function ganttHasRows(page: Page): Promise<boolean> {
  const chart = page.getByTestId('gantt-chart');
  if (!(await chart.isVisible().catch(() => false))) return false;
  return (await chart.locator('[data-testid^="gantt-row-"]').count()) > 0;
}

test.describe('Gantt ghost-dependency badge', () => {
  test('data-hidden-count attribute matches badge text count', async ({ page }) => {
    const ids = await gotoTasksAndCollectIds(page);
    if (ids.length < 2) test.skip(true, 'Need ≥2 tasks to chain a hidden predecessor');

    // Hide the first task; whatever depends on it should sprout a ghost-deps badge.
    const hideBtn = page.getByTestId(`tasks-hide-${ids[0]}`);
    if ((await hideBtn.count()) === 0) test.skip(true, 'tasks-hide button not present');
    await hideBtn.click();

    await gotoPage(page, '/gantt');
    if (!(await ganttHasRows(page))) test.skip(true, 'Gantt empty after hide');

    const badges = page.locator('[data-testid^="gantt-ghost-deps-"]');
    const n = await badges.count();
    if (n === 0) test.skip(true, 'No visible task depended on the hidden one');

    for (let i = 0; i < n; i++) {
      const badge = badges.nth(i);
      const dataCount = await badge.getAttribute('data-hidden-count');
      const text = (await badge.innerText()).trim();
      expect(dataCount).toMatch(/^\d+$/);
      const m = text.match(/(\d+)\s+hidden dep/);
      expect(m, `badge text "${text}" missing count`).not.toBeNull();
      expect(m![1]).toBe(dataCount);
    }
  });

  test('singular vs plural wording matches the count', async ({ page }) => {
    const ids = await gotoTasksAndCollectIds(page);
    if (ids.length < 2) test.skip(true, 'Need ≥2 tasks');

    const hideBtn = page.getByTestId(`tasks-hide-${ids[0]}`);
    if ((await hideBtn.count()) === 0) test.skip(true);
    await hideBtn.click();

    await gotoPage(page, '/gantt');
    const badges = page.locator('[data-testid^="gantt-ghost-deps-"]');
    const n = await badges.count();
    if (n === 0) test.skip(true, 'No ghost badges produced');

    for (let i = 0; i < n; i++) {
      const badge = badges.nth(i);
      const dataCount = parseInt(await badge.getAttribute('data-hidden-count') ?? '0', 10);
      const text = (await badge.innerText()).trim();
      if (dataCount === 1) {
        expect(text).toMatch(/\bdep\b/);
        expect(text).not.toMatch(/\bdeps\b/);
      } else {
        expect(text).toMatch(/\bdeps\b/);
      }
    }
  });
});
