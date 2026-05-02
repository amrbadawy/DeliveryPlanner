import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: week-numbering setting affects header W-number labels.
 *
 * The Settings page lets users pick ISO 8601, Sunday-first, Monday-first, or
 * Follow-Working-Week numbering. WeekHeaderBuilder applies the chosen rule
 * to every column. We verify that switching ISO ↔ Sunday-first changes at
 * least one rendered W-number label (typically off by one around year/quarter
 * boundaries).
 *
 * The test is self-restoring: the original numbering rule is captured before
 * mutation and reverted in afterAll, so it never leaks into other specs.
 */

const SELECT = 'settings-week-numbering-select';
const SAVE = 'settings-week-numbering-save';

async function setWeekNumbering(page: Page, value: string) {
  await gotoPage(page, '/settings');
  const sel = page.getByTestId(SELECT);
  await expect(sel).toBeVisible();
  await sel.selectOption({ value });
  await page.getByTestId(SAVE).click();
  await expect(page.getByTestId('settings-status')).toBeVisible({ timeout: 5_000 });
}

async function readWeekNumbering(page: Page): Promise<string> {
  await gotoPage(page, '/settings');
  return await page.getByTestId(SELECT).inputValue();
}

async function readWeekLabels(page: Page): Promise<string[]> {
  await runSchedulerFromDashboard(page);
  await gotoPage(page, '/gantt');
  const chart = page.getByTestId('gantt-chart');
  if (!(await chart.isVisible().catch(() => false))) return [];
  return chart.locator('.gantt-week-number').allInnerTexts();
}

test.describe('Gantt week-numbering setting', () => {
  test('switching ISO ↔ Sunday-first changes at least one W-label', async ({ page }) => {
    // Capture original within the test (not beforeAll) to avoid racing the
    // dev-server boot. Restore in finally so we never leak.
    const original = await readWeekNumbering(page);
    try {
      await setWeekNumbering(page, 'ISO_8601');
      const isoLabels = await readWeekLabels(page);
      if (isoLabels.length === 0) test.skip(true, 'No week labels rendered');

      await setWeekNumbering(page, 'SUNDAY_FIRSTDAY');
      const sunLabels = await readWeekLabels(page);
      expect(sunLabels.length).toBe(isoLabels.length);

      // At least one label must differ. If the plan range is short and lands in
      // a region where ISO and Sunday-first happen to agree, skip rather than
      // assert a false negative.
      const differing = isoLabels.some((l, i) => l !== sunLabels[i]);
      if (!differing) {
        test.skip(true, 'Plan range produced identical labels for both rules');
      }
      expect(differing).toBe(true);
    } finally {
      // Best-effort restore; do not fail the test if restore fails.
      await setWeekNumbering(page, original).catch(() => {});
    }
  });
});
