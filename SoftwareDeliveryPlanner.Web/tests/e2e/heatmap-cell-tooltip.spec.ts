import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * heatmap-cell-tooltip.spec.ts
 *
 * Each cell exposes a `title` attribute encoding resource name, week number,
 * date range, and one-decimal pct. Verifies the contract for hover discovery
 * (no programmatic tooltip widget, just the native title).
 */

const TITLE_RE = /^.+ \u2014 W\d{1,2} \(\d{2} \w{3} - \d{2} \w{3}\) \u2014 \d+\.\d%$/;

test.describe('Heatmap cell tooltip', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');
    await page.getByTestId('heatmap-refresh').click();
    const empty = page.getByTestId('heatmap-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }
    await expect(page.getByTestId('heatmap-table')).toBeVisible();
  });

  test('every cell title matches "<name> \u2014 W## (DD MMM - DD MMM) \u2014 N.N%"', async ({ page }) => {
    const cells = page.getByTestId('heatmap-table').locator('tbody td.heatmap-cell');
    const count = await cells.count();
    expect(count).toBeGreaterThan(0);

    // Sample up to 20 cells to keep runtime short on dense heatmaps.
    const sampleSize = Math.min(count, 20);
    for (let i = 0; i < sampleSize; i++) {
      const title = (await cells.nth(i).getAttribute('title')) ?? '';
      expect(title, `cell #${i}`).toMatch(TITLE_RE);
    }
  });

  test('cell title resource name matches its row label', async ({ page }) => {
    const rows = page.getByTestId('heatmap-table').locator('tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);

    const firstRow = rows.first();
    const rowName = ((await firstRow.locator('td.heatmap-resource-cell').textContent()) ?? '').trim();
    expect(rowName.length).toBeGreaterThan(0);

    const firstCell = firstRow.locator('td.heatmap-cell').first();
    const title = (await firstCell.getAttribute('title')) ?? '';
    // Title starts with the resource name, then a space-em-dash separator.
    expect(title.startsWith(`${rowName} \u2014 `)).toBe(true);
  });

  test('cell title pct matches the displayed integer percent (one-decimal vs zero-decimal)', async ({ page }) => {
    const cells = page.getByTestId('heatmap-table').locator('tbody td.heatmap-cell');
    const count = await cells.count();
    const sampleSize = Math.min(count, 10);
    for (let i = 0; i < sampleSize; i++) {
      const cell = cells.nth(i);
      const text = ((await cell.textContent()) ?? '').trim();          // e.g. "65%"
      const title = (await cell.getAttribute('title')) ?? '';            // ends with "65.4%"
      const titleMatch = title.match(/(\d+)\.\d%$/);
      expect(titleMatch, `title "${title}" should end with N.N%`).not.toBeNull();
      const titleIntPart = titleMatch![1];
      // Display rounds the pct (Math.Round, banker's). Title value rounded to
      // an integer must be within 1 of the displayed value to allow for the
      // standard half-to-even vs half-up rounding gap.
      const displayed = parseInt(text.replace('%', ''), 10);
      const titleInt = parseInt(titleIntPart, 10);
      const titleRoundedFromDecimal = Math.round(parseFloat(title.match(/(\d+\.\d)%$/)![1]));
      expect(Math.abs(displayed - titleRoundedFromDecimal), `displayed ${displayed}% vs title ${titleInt}.x%`).toBeLessThanOrEqual(1);
    }
  });
});
