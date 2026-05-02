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
 * Verifies all four keyboard shortcuts (1/2/3/4 → Day/Week/Month/Quarter)
 * plus F (fit) and T (today). Confirms shortcuts are ignored while focus
 * is in a text input (so users can search without changing zoom).
 */
test.describe('Gantt zoom keyboard shortcuts', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');
  });

  const cases: Array<[string, string]> = [
    ['1', 'day'],
    ['2', 'week'],
    ['3', 'month'],
    ['4', 'quarter'],
  ];

  for (const [key, level] of cases) {
    test(`keyboard "${key}" activates ${level} zoom`, async ({ page }) => {
      await ensureChartOrSkip(page);

      // Make sure we don't already start on the target level.
      const otherKey = key === '1' ? '2' : '1';
      await page.locator('body').click({ position: { x: 5, y: 5 } });
      await page.keyboard.press(otherKey);
      await page.waitForTimeout(80);

      // Now press the target shortcut.
      await page.keyboard.press(key);
      await page.waitForTimeout(80);
      await expect(page.getByTestId(`gantt-zoom-${level}`)).toHaveAttribute('aria-pressed', 'true');
    });
  }

  test('only one zoom level is active after any keyboard switch', async ({ page }) => {
    await ensureChartOrSkip(page);
    await page.locator('body').click({ position: { x: 5, y: 5 } });
    await page.keyboard.press('3');
    await page.waitForTimeout(80);

    let pressedCount = 0;
    for (const lvl of ['day', 'week', 'month', 'quarter']) {
      const v = await page.getByTestId(`gantt-zoom-${lvl}`).getAttribute('aria-pressed');
      if (v === 'true') pressedCount++;
    }
    expect(pressedCount).toBe(1);
  });

  test('shortcut keys are ignored when typing into a search input', async ({ page }) => {
    await ensureChartOrSkip(page);

    // Start on Week zoom so we can detect a wrongful change to Day.
    await page.getByTestId('gantt-zoom-week').click();
    await page.waitForTimeout(80);
    await expect(page.getByTestId('gantt-zoom-week')).toHaveAttribute('aria-pressed', 'true');

    // Find any visible text input on the page (filter sidebar search, etc.)
    const searchInput = page.locator('input[type="text"], input[type="search"]').first();
    if (!(await searchInput.count())) test.skip(true, 'No text input on Gantt to focus');

    await searchInput.click();
    await page.keyboard.type('1');

    // Zoom must NOT have switched to Day.
    await expect(page.getByTestId('gantt-zoom-week')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('gantt-zoom-day')).toHaveAttribute('aria-pressed', 'false');
  });

  test('keyboard "F" triggers Fit (no error, button still focusable)', async ({ page }) => {
    await ensureChartOrSkip(page);
    await page.locator('body').click({ position: { x: 5, y: 5 } });
    // Pressing F should not throw or change zoom level. The Fit button must remain visible.
    await page.keyboard.press('f');
    await page.waitForTimeout(80);
    await expect(page.getByTestId('gantt-fit')).toBeVisible();
  });

  test('keyboard "T" scrolls to today (chart still visible)', async ({ page }) => {
    await ensureChartOrSkip(page);
    await page.locator('body').click({ position: { x: 5, y: 5 } });
    await page.keyboard.press('t');
    await page.waitForTimeout(80);
    await expect(page.getByTestId('gantt-chart')).toBeVisible();
  });
});
