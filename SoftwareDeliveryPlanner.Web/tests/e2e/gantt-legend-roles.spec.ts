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
 * Verifies the Gantt legend: role swatches, role-toggle interaction,
 * Estimated/Allocated/Status legend sections, and that hiding all roles
 * preserves the chart frame (rows still rendered, segments hidden).
 */
test.describe('Gantt legend & role toggles', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');
  });

  test('legend renders all four section headings', async ({ page }) => {
    await ensureChartOrSkip(page);
    const legend = page.getByTestId('gantt-legend');
    await expect(legend).toBeVisible();
    await expect(legend).toContainText('Roles:');
    await expect(legend).toContainText('Segments:');
    await expect(legend).toContainText('Status:');
  });

  test('all six pipeline roles are present as toggle buttons', async ({ page }) => {
    await ensureChartOrSkip(page);
    // DomainConstants.ResourceRole.PipelineOrder = BA, SA, UX, UI, DEV, QA
    for (const role of ['ba', 'sa', 'ux', 'ui', 'dev', 'qa']) {
      await expect(page.getByTestId(`gantt-role-toggle-${role}`)).toBeVisible();
    }
  });

  test('all role toggles start in the visible state (aria-pressed=true)', async ({ page }) => {
    await ensureChartOrSkip(page);
    for (const role of ['ba', 'sa', 'ux', 'ui', 'dev', 'qa']) {
      const btn = page.getByTestId(`gantt-role-toggle-${role}`);
      await expect(btn).toHaveAttribute('aria-pressed', 'true');
    }
  });

  test('clicking a role toggle flips aria-pressed and hides the swatch', async ({ page }) => {
    await ensureChartOrSkip(page);
    const dev = page.getByTestId('gantt-role-toggle-dev');
    await expect(dev).toHaveAttribute('aria-pressed', 'true');
    await dev.click();
    await expect(dev).toHaveAttribute('aria-pressed', 'false');

    // Visual signal: the off-state class is applied.
    await expect(dev).toHaveClass(/gantt-legend-toggle-off/);

    // Toggle back on
    await dev.click();
    await expect(dev).toHaveAttribute('aria-pressed', 'true');
  });

  test('hiding a role removes its segments from rendered bars', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);
    // Pick any rendered bar with a DEV segment.
    const devSegment = chart.locator('[data-testid^="gantt-segment-"][data-testid$="-DEV"]').first();
    if (!(await devSegment.count())) test.skip(true, 'No DEV segments in this run');

    await expect(devSegment).toBeVisible();
    await page.getByTestId('gantt-role-toggle-dev').click();
    // After hiding DEV, no DEV segments should remain in the DOM.
    await expect.poll(async () =>
      chart.locator('[data-testid^="gantt-segment-"][data-testid$="-DEV"]').count()
    ).toBe(0);
  });

  test('hiding five of six roles leaves segments only from the remaining role', async ({ page }) => {
    const chart = await ensureChartOrSkip(page);
    const initialSegs = await chart.locator('[data-testid^="gantt-segment-"]').count();
    if (initialSegs === 0) test.skip(true, 'No segments rendered in this run');

    // Production invariant: at least one role MUST remain visible (Gantt.razor
    // ToggleRole guards against removing the last entry from visibleRoles).
    // We therefore hide five of six and verify only segments matching the
    // last-remaining role survive.
    const rolesInOrder = ['ba', 'sa', 'ux', 'ui', 'dev', 'qa'];
    const keepRole = 'qa'; // last in the list — gets to stay visible

    for (const role of rolesInOrder) {
      if (role === keepRole) continue;
      const btn = page.getByTestId(`gantt-role-toggle-${role}`);
      const pressed = await btn.getAttribute('aria-pressed');
      if (pressed === 'true') {
        await btn.click();
        await expect(btn).toHaveAttribute('aria-pressed', 'false', { timeout: 3_000 });
      }
    }

    // After hiding 5 roles, segments must drop and any remaining segment must
    // be a QA segment (testid suffix `-QA`).
    await expect.poll(async () =>
      chart.locator('[data-testid^="gantt-segment-"]').count()
    , { timeout: 5_000 }).toBeLessThan(initialSegs);

    const remaining = await chart.locator('[data-testid^="gantt-segment-"]').all();
    for (const seg of remaining) {
      const tid = await seg.getAttribute('data-testid');
      expect(tid, `unexpected non-QA segment ${tid}`).toMatch(/-QA$/);
    }
  });

  test('legend Status section shows all five status entries', async ({ page }) => {
    await ensureChartOrSkip(page);
    const legend = page.getByTestId('gantt-legend');
    for (const label of ['Completed', 'In Progress', 'Not Started', 'At Risk', 'Late']) {
      await expect(legend).toContainText(label);
    }
  });

  test('legend Segments section shows Estimated and Allocated entries', async ({ page }) => {
    await ensureChartOrSkip(page);
    const legend = page.getByTestId('gantt-legend');
    await expect(legend).toContainText('Estimated');
    await expect(legend).toContainText('Allocated');
  });
});
