import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: strict-deadline marker contract.
 *
 * Existing gantt.spec.ts asserts the marker exists when a task has a deadline.
 * These tests pin down the per-marker invariants:
 *   - exactly one of `gantt-strict-ok` (delta ≥ 0) or `gantt-strict-overdue`
 *     (delta < 0) is on the marker; never both, never neither
 *   - the flag text matches +Nd / -Nd / 0d sign convention
 *   - title attribute contains "Deadline:" and either "buffer" or "overdue"
 *     consistent with the class
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

test.describe('Gantt strict-deadline markers', () => {
  test('every strict marker has exactly one of -ok / -overdue class', async ({ page }) => {
    const chart = await gotoGantt(page);
    const markers = chart.locator('[data-testid^="gantt-strict-"]');
    const n = await markers.count();
    if (n === 0) test.skip(true, 'No strict markers rendered');

    const classes = await markers.evaluateAll(els => els.map(e => e.className));
    for (const cls of classes) {
      const hasOk = cls.includes('gantt-strict-ok');
      const hasOverdue = cls.includes('gantt-strict-overdue');
      expect(hasOk !== hasOverdue, `marker class ambiguous: ${cls}`).toBe(true);
    }
  });

  test('flag text and class agree on sign convention', async ({ page }) => {
    const chart = await gotoGantt(page);
    const markers = chart.locator('[data-testid^="gantt-strict-"]');
    const n = await markers.count();
    if (n === 0) test.skip(true);

    for (let i = 0; i < n; i++) {
      const m = markers.nth(i);
      const cls = await m.evaluate(e => e.className);
      const flag = ((await m.locator('.gantt-strict-flag').innerText()).trim());
      const isOk = cls.includes('gantt-strict-ok');
      // Flag is "+Nd" or "0d" when ok (delta ≥ 0); "-Nd" when overdue.
      if (isOk) {
        expect(flag, `ok marker flag "${flag}" should not start with "-"`).not.toMatch(/^-/);
      } else {
        expect(flag, `overdue marker flag "${flag}" should start with "-"`).toMatch(/^-/);
      }
      expect(flag).toMatch(/d$/);
    }
  });

  test('title attribute is consistent with the class', async ({ page }) => {
    const chart = await gotoGantt(page);
    const markers = chart.locator('[data-testid^="gantt-strict-"]');
    const n = await markers.count();
    if (n === 0) test.skip(true);

    for (let i = 0; i < n; i++) {
      const m = markers.nth(i);
      const cls = await m.evaluate(e => e.className);
      const title = await m.getAttribute('title') ?? '';
      expect(title).toContain('Deadline:');
      if (cls.includes('gantt-strict-ok')) {
        expect(title, `ok marker title missing "buffer": ${title}`).toContain('buffer');
      } else {
        expect(title, `overdue marker title missing "overdue": ${title}`).toContain('overdue');
      }
    }
  });
});
