import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * heatmap-colors.spec.ts
 *
 * Verifies the inline style on every heatmap cell uses one of the five
 * documented CSS custom properties from HeatmapColorPalette, and that the
 * legend swatches expose the same set. No overlap with heatmap.spec.ts which
 * only checks legend visibility, not its contents.
 */

const ALLOWED_VARS = [
  'var(--color-bg-secondary)',
  'var(--color-info-light)',
  'var(--color-success-light)',
  'var(--color-warning-light)',
  'var(--color-danger-light)',
];

test.describe('Heatmap cell colours', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/heatmap');
    await page.getByTestId('heatmap-refresh').click();
  });

  test('every cell background-color uses one of the five palette variables', async ({ page }) => {
    const table = page.getByTestId('heatmap-table');
    const empty = page.getByTestId('heatmap-empty');
    await expect.poll(async () => {
      return (await table.isVisible().catch(() => false)) || (await empty.isVisible().catch(() => false));
    }, { timeout: 15_000 }).toBeTruthy();
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }

    const cells = table.locator('tbody td.heatmap-cell');
    const count = await cells.count();
    expect(count).toBeGreaterThan(0);

    // Pull the raw style attribute (which contains the var(...) expression
    // verbatim) so we assert against the *source-of-truth* contract, not the
    // browser-resolved RGB which depends on the active theme.
    const styles: string[] = [];
    for (let i = 0; i < count; i++) {
      const s = (await cells.nth(i).getAttribute('style')) ?? '';
      styles.push(s);
    }

    for (const s of styles) {
      const matched = ALLOWED_VARS.some((v) => s.includes(v));
      expect(matched, `cell style "${s}" must use one of the palette vars`).toBe(true);
    }
  });

  test('legend exposes all five palette variables in order', async ({ page }) => {
    const table = page.getByTestId('heatmap-table');
    const empty = page.getByTestId('heatmap-empty');
    await expect.poll(async () => {
      return (await table.isVisible().catch(() => false)) || (await empty.isVisible().catch(() => false));
    }, { timeout: 15_000 }).toBeTruthy();

    const legend = page.getByTestId('heatmap-legend');
    await expect(legend).toBeVisible();

    const swatches = legend.locator('.legend-swatch');
    await expect(swatches).toHaveCount(ALLOWED_VARS.length);

    for (let i = 0; i < ALLOWED_VARS.length; i++) {
      const style = (await swatches.nth(i).getAttribute('style')) ?? '';
      expect(style, `legend swatch #${i} should use ${ALLOWED_VARS[i]}`).toContain(ALLOWED_VARS[i]);
    }
  });

  test('cells with matching pct text resolve to identical computed background', async ({ page }) => {
    // Cells that display the same percent label MUST resolve to the same RGB
    // — invariant of the bucket function being deterministic.
    const table = page.getByTestId('heatmap-table');
    const empty = page.getByTestId('heatmap-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'No heatmap data in this run');
    }
    await expect(table).toBeVisible();

    const cells = table.locator('tbody td.heatmap-cell');
    const total = await cells.count();
    if (total < 2) test.skip(true, 'Need at least 2 cells to compare');

    type CellInfo = { text: string; bg: string };
    const infos: CellInfo[] = [];
    for (let i = 0; i < total; i++) {
      const c = cells.nth(i);
      const text = ((await c.textContent()) ?? '').trim();
      const bg = await c.evaluate((el) => getComputedStyle(el as HTMLElement).backgroundColor);
      infos.push({ text, bg });
    }

    // Group by displayed pct text; every group must have exactly one bg colour.
    const grouped = new Map<string, Set<string>>();
    for (const { text, bg } of infos) {
      if (!grouped.has(text)) grouped.set(text, new Set());
      grouped.get(text)!.add(bg);
    }
    for (const [text, bgs] of grouped.entries()) {
      expect(bgs.size, `cells displaying "${text}" should share a single bg, got ${[...bgs].join(', ')}`).toBe(1);
    }
  });
});
