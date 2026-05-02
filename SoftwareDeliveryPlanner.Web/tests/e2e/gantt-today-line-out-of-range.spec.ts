import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: today-line conditional rendering.
 *
 * Gantt.razor only renders <div data-testid="gantt-today"> when today falls
 * within [planStart, planEnd]. These tests pin down the contract:
 *   - the today line, when present, is positioned via a calc(...) expression
 *     that includes the 240px label-column offset
 *   - aria-label / title contain today's ISO date
 *   - the line's bounding box sits horizontally inside the chart container
 *
 * Mutating plan dates to push today out of range would require either a UI
 * we don't have or a destructive seeder reset; instead we assert the
 * conditional contract using whatever the live plan range happens to be.
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

function todayIso(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

test.describe('Gantt today-line', () => {
  test('today line, when rendered, has expected attributes and offset', async ({ page }) => {
    const chart = await gotoGantt(page);
    const today = chart.getByTestId('gantt-today');
    const present = (await today.count()) > 0;

    if (!present) {
      // Plan range excludes today — nothing to verify positively.
      // Confirm the negative: no orphan today-line elements anywhere.
      expect(await page.locator('[data-testid="gantt-today"]').count()).toBe(0);
      return;
    }

    // title attribute should contain today's ISO date.
    await expect(today).toHaveAttribute('title', new RegExp(todayIso()));

    // style attribute should include calc( and the 240px label-column offset.
    const style = await today.getAttribute('style') ?? '';
    expect(style).toContain('calc(');
    expect(style).toContain('240px');
    expect(style).toMatch(/left\s*:/i);
  });

  test('today line is positioned within the chart horizontal bounds', async ({ page }) => {
    const chart = await gotoGantt(page);
    const today = chart.getByTestId('gantt-today');
    if ((await today.count()) === 0) {
      test.skip(true, 'Today is outside the current plan range');
    }
    const chartBox = await chart.boundingBox();
    const todayBox = await today.boundingBox();
    expect(chartBox).not.toBeNull();
    expect(todayBox).not.toBeNull();
    expect(todayBox!.x).toBeGreaterThanOrEqual(chartBox!.x);
    expect(todayBox!.x).toBeLessThanOrEqual(chartBox!.x + chartBox!.width);
  });
});
