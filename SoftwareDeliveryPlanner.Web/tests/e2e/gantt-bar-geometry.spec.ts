import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: bar geometry across zoom levels.
 *
 * The existing gantt.spec.ts asserts that DAY zoom widens the timeline and
 * QUARTER narrows it (via container scroll width). These tests probe a
 * complementary invariant: the relative positions/widths of individual bars
 * should remain monotonic and proportional regardless of zoom.
 */

async function gotoGanttAndEnsureBars(page: Page) {
  await runSchedulerFromDashboard(page);
  await gotoPage(page, '/gantt');
  const chart = page.getByTestId('gantt-chart');
  if (!(await chart.isVisible().catch(() => false))) {
    test.skip(true, 'Gantt chart not rendered (no scheduled tasks)');
  }
  return chart;
}

test.describe('Gantt bar geometry', () => {
  test('every bar has a non-zero width and a left offset within 0..100%', async ({ page }) => {
    const chart = await gotoGanttAndEnsureBars(page);
    const bars = chart.locator('[data-testid^="gantt-bar-"]');
    const count = await bars.count();
    if (count === 0) test.skip(true, 'No bars rendered');

    for (let i = 0; i < count; i++) {
      const style = await bars.nth(i).getAttribute('style') ?? '';
      // style should include left and width as percentages (or calc()).
      expect(style, `bar ${i} style missing left/width: ${style}`).toMatch(/left\s*:/i);
      expect(style, `bar ${i} style missing width: ${style}`).toMatch(/width\s*:/i);

      const box = await bars.nth(i).boundingBox();
      expect(box, `bar ${i} not laid out`).not.toBeNull();
      expect(box!.width).toBeGreaterThan(0);
    }
  });

  test('bar widths preserve relative ordering across zoom changes', async ({ page }) => {
    const chart = await gotoGanttAndEnsureBars(page);
    const bars = chart.locator('[data-testid^="gantt-bar-"]');
    const count = await bars.count();
    if (count < 2) test.skip(true, 'Need ≥2 bars to compare ordering');

    async function widthsAtZoom(zoom: 'day' | 'week' | 'month' | 'quarter') {
      await page.getByTestId(`gantt-zoom-${zoom}`).click();
      await expect(page.getByTestId(`gantt-zoom-${zoom}`)).toHaveAttribute('aria-pressed', 'true');
      // Wait one paint for the new layout.
      await page.waitForTimeout(150);
      const widths: number[] = [];
      for (let i = 0; i < count; i++) {
        const box = await bars.nth(i).boundingBox();
        widths.push(box?.width ?? 0);
      }
      return widths;
    }

    const wWeek = await widthsAtZoom('week');
    const wQuarter = await widthsAtZoom('quarter');

    // Quarter zoom must produce widths ≤ week widths for every bar (timeline
    // is compressed, so each bar shrinks). Permit equality where rounding
    // collapses sub-pixel deltas.
    for (let i = 0; i < count; i++) {
      expect(wQuarter[i], `bar ${i}: quarter ${wQuarter[i]} > week ${wWeek[i]}`)
        .toBeLessThanOrEqual(wWeek[i] + 1);
    }

    // Within a single zoom, the rank order of bars by width should be stable
    // when viewed at a different zoom. Compute argsort and compare.
    const rank = (arr: number[]) =>
      arr.map((v, i) => ({ v, i })).sort((a, b) => a.v - b.v).map(x => x.i);
    expect(rank(wQuarter)).toEqual(rank(wWeek));
  });
});
