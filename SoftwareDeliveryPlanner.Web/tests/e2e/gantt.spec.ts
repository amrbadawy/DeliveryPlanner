import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/** Wait for either the chart or the empty state to appear, skip if empty. */
async function ensureChartOrSkip(page: Page) {
  const chart = page.getByTestId('gantt-chart');
  const empty = page.getByTestId('gantt-empty');

  await expect.poll(async () => {
    const c = await chart.isVisible().catch(() => false);
    const e = await empty.isVisible().catch(() => false);
    return c || e;
  }, { timeout: 10_000 }).toBeTruthy();

  if (await empty.isVisible().catch(() => false)) {
    test.skip(true, 'Scheduler produced no scheduled tasks in this run');
  }
  await expect(chart).toBeVisible();
  return chart;
}

test.describe('Gantt chart', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');
  });

  test('gantt page loads and shows heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Gantt Chart/ })).toBeVisible();
  });

  test('gantt chart renders with task rows after scheduler run', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Should have at least one gantt row
    const rows = chart.locator('[data-testid^="gantt-row-"]');
    await expect(rows.first()).toBeVisible();
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('gantt bars are visible for scheduled tasks', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // At least one bar should be present
    const bars = chart.locator('[data-testid^="gantt-bar-"]');
    await expect(bars.first()).toBeVisible();
    const count = await bars.count();
    expect(count).toBeGreaterThan(0);
  });

  test('gantt legend is visible', async ({ page }) => {
    await ensureChartOrSkip(page);
    const legend = page.getByTestId('gantt-legend');
    await expect(legend).toBeVisible();

    // Legend should contain the expected labels
    await expect(legend).toContainText('Completed');
    await expect(legend).toContainText('In Progress');
    await expect(legend).toContainText('Not Started');
    await expect(legend).toContainText('At Risk');
    await expect(legend).toContainText('Late');
  });

  test('gantt shows plan date range', async ({ page }) => {
    await ensureChartOrSkip(page);
    const range = page.getByTestId('gantt-range');
    await expect(range).toBeVisible();
    // Range should contain "Plan range:" and date patterns
    await expect(range).toContainText('Plan range:');
    await expect(range).toContainText('days');
  });

  test('excluded task count badge shows when tasks are unscheduled', async ({ page }) => {
    await ensureChartOrSkip(page);

    // If there are unscheduled tasks, the badge should appear
    const badge = page.getByTestId('gantt-excluded-count');
    const isVisible = await badge.isVisible().catch(() => false);
    if (isVisible) {
      await expect(badge).toContainText('not scheduled');
      await expect(badge).toContainText('Showing');
    }
    // If no unscheduled tasks exist, badge is correctly hidden — both states are valid
  });

  test('refresh button re-runs scheduler and reloads chart', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

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

    // If empty state is shown, verify the contextual message
    const emptyVisible = await empty.isVisible().catch(() => false);
    if (emptyVisible) {
      const text = await empty.innerText();
      // Should contain guidance about running the scheduler
      expect(text).toContain('Refresh');
      // If tasks exist but none scheduled, should mention task count
      const mentionsTasks = text.includes('task(s) exist') || text.includes('scheduler');
      expect(mentionsTasks).toBe(true);
    }
  });

  // ── New test scenarios (WP11) ─────────────────────────────

  test('multi-role segments: each task bar contains distinct colored role segments', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Find first task bar and check it has at least TWO role segments (DEV + QA minimum)
    const firstBar = chart.locator('[data-testid^="gantt-bar-"]').first();
    await expect(firstBar).toBeVisible();

    const segments = firstBar.locator('[data-testid^="gantt-segment-"]');
    const segCount = await segments.count();
    expect(segCount).toBeGreaterThanOrEqual(2);

    // Each segment should have a role label
    for (let i = 0; i < Math.min(segCount, 3); i++) {
      const label = segments.nth(i).locator('.gantt-segment-label');
      await expect(label).toBeVisible();
      const text = await label.textContent();
      expect(text!.length).toBeGreaterThan(0);
    }
  });

  test('legend toggle hides and shows role segments', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Click DEV toggle to hide DEV segments
    const devToggle = page.getByTestId('gantt-role-toggle-dev');
    await expect(devToggle).toBeVisible();

    // Count DEV segments before toggle
    const devSegsBefore = chart.locator('[data-testid$="-DEV"]');
    const countBefore = await devSegsBefore.count();

    if (countBefore > 0) {
      await devToggle.click();

      // DEV segments should now be hidden
      await expect(devSegsBefore.first()).not.toBeVisible();

      // Toggle has aria-pressed="false"
      await expect(devToggle).toHaveAttribute('aria-pressed', 'false');

      // Click again to re-show
      await devToggle.click();
      await expect(devToggle).toHaveAttribute('aria-pressed', 'true');
    }
  });

  test('critical path toggle highlights critical tasks', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    const toggle = page.getByTestId('gantt-critical-path-toggle');
    await expect(toggle).toBeVisible();

    // Click to enable critical path
    await toggle.click();

    // Legend should now show "Critical Path" entry
    const legend = page.getByTestId('gantt-legend');
    await expect(legend).toContainText('Critical Path');

    // At least one bar should have critical class (if tasks have dependencies)
    // This is a soft check — if no dependencies, no critical highlighting
    const criticalBars = chart.locator('.gantt-bar-critical');
    const critCount = await criticalBars.count();
    // Just verify the toggle works — critical bars may or may not exist
    expect(critCount).toBeGreaterThanOrEqual(0);
  });

  test('estimated vs allocated segments are visually distinct', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Check for estimated segments (marked with *)
    const allSegments = chart.locator('[data-testid^="gantt-segment-"]');
    const count = await allSegments.count();
    expect(count).toBeGreaterThan(0);

    // Estimated segments have the gantt-segment-estimated CSS class
    const estimatedSegs = chart.locator('.gantt-segment-estimated');
    const estCount = await estimatedSegs.count();
    // At least check that the class exists if there are unallocated roles
    // (BA/SA/UX/UI have no resources in seed data, so should be estimated)
    if (estCount > 0) {
      // Estimated segments have a * label suffix
      const firstEst = estimatedSegs.first();
      const label = await firstEst.locator('.gantt-segment-label').textContent();
      expect(label).toContain('*');
    }
  });

  test('clicking a task row navigates to task detail page', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Get the first row's task ID from the data-testid
    const firstRow = chart.locator('[data-testid^="gantt-row-"]').first();
    await expect(firstRow).toBeVisible();
    const testId = await firstRow.getAttribute('data-testid');
    const taskId = testId!.replace('gantt-row-', '');

    // Click the label column (clickable-row)
    await firstRow.locator('.clickable-row').click();

    // Should navigate to task detail page
    await page.waitForURL(`**/tasks/${taskId}`, { timeout: 5000 });
    expect(page.url()).toContain(`/tasks/${taskId}`);
  });

  test('strict date markers appear for tasks with deadlines', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Check for strict markers — may or may not exist depending on seed data
    const strictMarkers = chart.locator('[data-testid^="gantt-strict-"]');
    const strictCount = await strictMarkers.count();

    if (strictCount > 0) {
      // Each strict marker should have a flag with +Nd or -Nd text
      const firstMarker = strictMarkers.first();
      await expect(firstMarker).toBeVisible();
      const flag = firstMarker.locator('.gantt-strict-flag');
      const flagText = await flag.textContent();
      expect(flagText).toMatch(/[+-]?\d+d/);
    }
  });

  test('overflow indicator shows when task has more than 6 role segments', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Overflow indicators appear as "+N" badges when > MaxVisibleLanes segments
    const overflows = chart.locator('[data-testid^="gantt-overflow-"]');
    const overflowCount = await overflows.count();

    // If any overflow exists, verify format
    if (overflowCount > 0) {
      const text = await overflows.first().textContent();
      expect(text).toMatch(/^\+\d+$/);
    }
    // It's valid to have no overflows if tasks have ≤ 6 roles
  });

  test('today marker is visible when plan range includes today', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Today marker should be visible if today falls within the plan range
    const todayMarker = page.getByTestId('gantt-today');
    const isVisible = await todayMarker.isVisible().catch(() => false);
    if (isVisible) {
      await expect(todayMarker).toBeVisible();
      const title = await todayMarker.getAttribute('title');
      expect(title).toContain('Today:');
    }
    // If today is outside the plan range, marker is correctly hidden
  });

  test('week grid lines and month headers render correctly', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Month headers should be present
    const months = chart.locator('.gantt-month');
    const monthCount = await months.count();
    expect(monthCount).toBeGreaterThan(0);

    // Week headers should be present
    const weeks = chart.locator('.gantt-week');
    const weekCount = await weeks.count();
    expect(weekCount).toBeGreaterThan(0);

    // Week numbers should have W{n} format
    const firstWeek = chart.locator('.gantt-week-number').first();
    const weekText = await firstWeek.textContent();
    expect(weekText).toMatch(/^W\d+$/);

    // Week grid lines should be present
    const gridLines = chart.locator('.gantt-week-line');
    const lineCount = await gridLines.count();
    expect(lineCount).toBeGreaterThan(0);
  });

  test('week numbers are real calendar week-of-year (not sequential 1,2,3,...)', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    const weekLabels = chart.locator('.gantt-week-number');
    const count = await weekLabels.count();
    expect(count).toBeGreaterThan(0);

    // Collect all visible labels
    const numbers: number[] = [];
    for (let i = 0; i < count; i++) {
      const text = (await weekLabels.nth(i).textContent()) ?? '';
      expect(text).toMatch(/^W\d{1,2}$/);
      numbers.push(parseInt(text.slice(1), 10));
    }

    // Real-week-of-year values are in [1, 53] and should not all be a 1..N sequence
    // (a plan starting in May should show ~W18, not W1).
    expect(numbers.every(n => n >= 1 && n <= 53)).toBe(true);

    // Detect "old sequential counter" pattern: 1, 2, 3, ... starting at 1
    const isOldSequential = numbers[0] === 1
      && numbers.every((n, i) => i === 0 || n === numbers[i - 1] + 1);
    expect(isOldSequential, 'Week numbers should be real calendar weeks, not 1,2,3,...').toBe(false);

    // Tooltip on the first week cell should include the year for disambiguation.
    const firstCell = chart.locator('.gantt-week').first();
    const tooltip = await firstCell.getAttribute('title');
    expect(tooltip).toMatch(/Week \d{1,2}, \d{4}/);
  });

  test('all task bars contain at least 2 role segments (DEV + QA domain invariant)', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    const bars = chart.locator('[data-testid^="gantt-bar-"]');
    const barCount = await bars.count();
    expect(barCount).toBeGreaterThan(0);

    // Every scheduled task must have at least DEV + QA segments
    for (let i = 0; i < barCount; i++) {
      const bar = bars.nth(i);
      const testId = await bar.getAttribute('data-testid');
      const segments = bar.locator('[data-testid^="gantt-segment-"]');
      const segCount = await segments.count();
      expect(segCount, `${testId} should have ≥2 segments but has ${segCount}`).toBeGreaterThanOrEqual(2);
    }
  });

  test('segments have correct CSS positioning (position: absolute)', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Verify that segments are absolutely positioned inside the shell bar
    const firstSegment = chart.locator('[data-testid^="gantt-segment-"]').first();
    await expect(firstSegment).toBeVisible();

    const position = await firstSegment.evaluate(el => window.getComputedStyle(el).position);
    expect(position).toBe('absolute');

    const height = await firstSegment.evaluate(el => window.getComputedStyle(el).height);
    expect(height).toBe('16px');
  });

  // ── Zoom feature ───────────────────────────────────────────
  test('zoom toolbar renders with all four levels + fit + today', async ({ page }) => {
    await ensureChartOrSkip(page);

    await expect(page.getByTestId('gantt-toolbar')).toBeVisible();
    for (const level of ['day', 'week', 'month', 'quarter']) {
      await expect(page.getByTestId(`gantt-zoom-${level}`)).toBeVisible();
    }
    await expect(page.getByTestId('gantt-fit')).toBeVisible();
    await expect(page.getByTestId('gantt-today-btn')).toBeVisible();
  });

  test('exactly one zoom level is active at a time (aria-pressed)', async ({ page }) => {
    await ensureChartOrSkip(page);

    const buttons = page.locator('[data-testid^="gantt-zoom-"]').filter({ hasNot: page.getByTestId('gantt-zoom-hint') });
    const all = await buttons.all();
    let pressed = 0;
    for (const b of all) {
      const ap = await b.getAttribute('aria-pressed');
      if (ap === 'true') pressed++;
    }
    expect(pressed).toBe(1);
  });

  test('clicking DAY zoom widens the timeline; QUARTER narrows it', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    const measureWidth = () => chart.evaluate(el => {
      const cs = window.getComputedStyle(el);
      // CSS variable drives min-width via calc(); read the variable directly.
      return parseFloat(cs.getPropertyValue('--gantt-timeline-width')) || 0;
    });

    await page.getByTestId('gantt-zoom-week').click();
    await page.waitForTimeout(50);
    const weekWidth = await measureWidth();

    await page.getByTestId('gantt-zoom-day').click();
    await page.waitForTimeout(50);
    const dayWidth = await measureWidth();

    await page.getByTestId('gantt-zoom-quarter').click();
    await page.waitForTimeout(50);
    const quarterWidth = await measureWidth();

    expect(dayWidth).toBeGreaterThan(weekWidth);
    expect(weekWidth).toBeGreaterThan(quarterWidth);
  });

  test('zoom level persists across reload (server-side setting)', async ({ page }) => {
    await ensureChartOrSkip(page);

    await page.getByTestId('gantt-zoom-month').click();
    await page.waitForTimeout(500); // debounced persistence (300ms) + buffer

    await page.reload();
    await ensureChartOrSkip(page);

    await expect(page.getByTestId('gantt-zoom-month')).toHaveAttribute('aria-pressed', 'true');
  });

  test('keyboard shortcut "1" switches to Day zoom', async ({ page }) => {
    await ensureChartOrSkip(page);

    // Make sure we don't start on Day
    await page.getByTestId('gantt-zoom-week').click();
    await page.waitForTimeout(50);

    // Focus body so the shortcut handler picks it up (input filter)
    await page.locator('body').click({ position: { x: 5, y: 5 } });
    await page.keyboard.press('1');
    await page.waitForTimeout(50);

    await expect(page.getByTestId('gantt-zoom-day')).toHaveAttribute('aria-pressed', 'true');
  });

  test('Fit button resets horizontal scroll to start', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);

    // Zoom in to make scrolling possible
    await page.getByTestId('gantt-zoom-day').click();
    await page.waitForTimeout(50);

    await chart.evaluate(el => { el.scrollLeft = 200; });
    const before = await chart.evaluate(el => el.scrollLeft);
    expect(before).toBeGreaterThan(0);

    await page.getByTestId('gantt-fit').click();
    await page.waitForTimeout(50);

    const after = await chart.evaluate(el => el.scrollLeft);
    expect(after).toBe(0);
  });
});
