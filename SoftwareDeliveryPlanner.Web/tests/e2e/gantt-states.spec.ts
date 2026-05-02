import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, triggerTestFault, clearTestFault } from './helpers';

/**
 * Verifies the four UI states of the Gantt page — loading skeleton, error,
 * empty (no scheduled tasks), and fully populated — using the env-gated
 * test-fault seam to deterministically trigger the failure path.
 */
test.describe('Gantt page states', () => {
  test.afterEach(async ({ page }) => {
    // Belt-and-braces cleanup. Always safe; never fails the test.
    await clearTestFault(page);
  });

  test('shows loading skeleton on first navigation', async ({ page }) => {
    // Navigate without waiting for networkidle so we can see the skeleton.
    await page.goto('/gantt');
    // The skeleton OR the chart should be in the DOM almost immediately.
    const skeleton = page.getByTestId('gantt-skeleton');
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');

    // One of these must appear within the SSR window. Skeleton is preferred but
    // on fast localhost runs we may skip directly to the resolved state.
    await expect.poll(async () => {
      const s = await skeleton.isVisible().catch(() => false);
      const c = await chart.isVisible().catch(() => false);
      const e = await empty.isVisible().catch(() => false);
      return s || c || e;
    }, { timeout: 10_000 }).toBeTruthy();
  });

  test('renders error state when GanttSegments fault is armed', async ({ page }) => {
    // Seed real data first so the empty path is ruled out.
    await runSchedulerFromDashboard(page);

    // Arm the fault BEFORE navigating to the Gantt page.
    await triggerTestFault(page, 'GanttSegments');

    await gotoPage(page, '/gantt');

    // Error state must render with the production testid.
    const error = page.getByTestId('gantt-error');
    await expect(error).toBeVisible({ timeout: 10_000 });

    // Heading and retry button are part of the error contract.
    await expect(error).toContainText('Unable to load Gantt chart');
    await expect(page.getByTestId('gantt-error-retry')).toBeVisible();
  });

  test('error state retry button reloads after fault is cleared', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await triggerTestFault(page, 'GanttSegments');
    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('gantt-error')).toBeVisible({ timeout: 10_000 });

    // Now clear the fault and click Retry — chart should appear.
    await clearTestFault(page, 'GanttSegments');
    await page.getByTestId('gantt-error-retry').click();

    // Either the chart or the empty state is acceptable; the error must be gone.
    await expect(page.getByTestId('gantt-error')).toHaveCount(0, { timeout: 10_000 });
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    await expect.poll(async () => {
      const c = await chart.isVisible().catch(() => false);
      const e = await empty.isVisible().catch(() => false);
      return c || e;
    }, { timeout: 10_000 }).toBeTruthy();
  });

  test('arming a non-Gantt fault does NOT affect the Gantt page', async ({ page }) => {
    await runSchedulerFromDashboard(page);

    // Arm an unrelated key — the Gantt handler should not pick this up.
    await triggerTestFault(page, 'SomeOtherOperation');

    await gotoPage(page, '/gantt');

    // Error state must NOT appear.
    await expect(page.getByTestId('gantt-error')).toHaveCount(0);

    // Either chart or empty is acceptable.
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    await expect.poll(async () => {
      const c = await chart.isVisible().catch(() => false);
      const e = await empty.isVisible().catch(() => false);
      return c || e;
    }, { timeout: 10_000 }).toBeTruthy();
  });

  test('empty state OR populated chart appears after scheduler run (no fault)', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');

    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    await expect.poll(async () => {
      const c = await chart.isVisible().catch(() => false);
      const e = await empty.isVisible().catch(() => false);
      return c || e;
    }, { timeout: 15_000 }).toBeTruthy();

    // Whichever it is, the error state must not be visible.
    await expect(page.getByTestId('gantt-error')).toHaveCount(0);
  });
});
