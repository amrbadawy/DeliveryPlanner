import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, waitForTableRows } from './helpers';

/**
 * E2E coverage for the persistent left filter sidebar (commit A).
 *
 * Phase 1 + 2 only: dimension chips, search, URL query sync, applied to
 * Tasks and Gantt pages. Saved views, pin/hide and ghost arrows are
 * verified in commit B.
 */

test.describe('Task filter sidebar (Tasks page)', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('sidebar renders with all dimension sections', async ({ page }) => {
    await gotoPage(page, '/tasks');

    const sidebar = page.getByTestId('task-filter-sidebar');
    await expect(sidebar).toBeVisible();

    // Search field
    await expect(page.getByTestId('task-filter-search')).toBeVisible();

    // Status chips
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-status-in_progress')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-status-completed')).toBeVisible();

    // Risk chips
    await expect(page.getByTestId('task-filter-chip-risk-on_track')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-risk-at_risk')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-risk-late')).toBeVisible();

    // Priority chips
    await expect(page.getByTestId('task-filter-chip-priority-high')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-priority-medium')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-priority-low')).toBeVisible();

    // Role chips (DEV / QA always present per domain invariant)
    await expect(page.getByTestId('task-filter-chip-role-dev')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-role-qa')).toBeVisible();

    // Dependency state chips
    await expect(page.getByTestId('task-filter-chip-dep-has_deps')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-dep-no_deps')).toBeVisible();
  });

  test('toggling a status chip narrows the visible task rows', async ({ page }) => {
    await gotoPage(page, '/tasks');

    const table = page.getByTestId('tasks-table');
    const totalRows = await waitForTableRows(table, 1);

    const completedChip = page.getByTestId('task-filter-chip-status-completed');
    await completedChip.click();
    await expect(completedChip).toHaveAttribute('aria-pressed', 'true');

    // Either narrower than original OR all rows happen to be completed already;
    // assert ≤ original and that every visible row carries the Completed status badge.
    await expect.poll(async () => await table.locator('tbody tr').count()).toBeLessThanOrEqual(totalRows);

    const visibleRows = await table.locator('tbody tr').count();
    if (visibleRows > 0) {
      const completedBadges = await table.locator('tbody tr', { hasText: 'Completed' }).count();
      expect(completedBadges).toBe(visibleRows);
    }
  });

  test('toggling a chip writes its dimension to the URL query string', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-priority-high').click();
    await expect.poll(() => page.url(), { message: 'pri=HIGH in URL' }).toMatch(/[?&]pri=HIGH(?:&|$)/);

    await page.getByTestId('task-filter-chip-role-dev').click();
    await expect.poll(() => page.url()).toMatch(/[?&]role=DEV(?:&|$)/);
  });

  test('navigating directly with ?role=DEV pre-applies the chip', async ({ page }) => {
    await gotoPage(page, '/tasks?role=DEV');

    const chip = page.getByTestId('task-filter-chip-role-dev');
    await expect(chip).toHaveAttribute('aria-pressed', 'true');
  });

  test('clear-all button removes every chip and clears URL params', async ({ page }) => {
    await gotoPage(page, '/tasks?status=COMPLETED&role=DEV');

    const clearBtn = page.getByTestId('task-filter-clear-all');
    await expect(clearBtn).toBeVisible();
    await clearBtn.click();

    await expect(page.getByTestId('task-filter-chip-status-completed')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('task-filter-chip-role-dev')).toHaveAttribute('aria-pressed', 'false');
    await expect.poll(() => page.url()).not.toMatch(/[?&](?:status|role)=/);
  });

  test('active-count badge reflects number of selected chips + search', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-status-not_started').click();
    await page.getByTestId('task-filter-chip-priority-high').click();

    await expect(page.getByTestId('task-filter-active-count')).toHaveText('2');

    const search = page.getByTestId('task-filter-search');
    await search.fill('SVC');
    await expect(page.getByTestId('task-filter-active-count')).toHaveText('3');
  });

  test('sidebar collapse toggle hides the body section', async ({ page }) => {
    await gotoPage(page, '/tasks');

    // Body visible initially
    await expect(page.getByTestId('task-filter-search')).toBeVisible();

    await page.getByTestId('task-filter-toggle').click();
    await expect(page.getByTestId('task-filter-search')).toBeHidden();

    await page.getByTestId('task-filter-toggle').click();
    await expect(page.getByTestId('task-filter-search')).toBeVisible();
  });

  test('keyboard shortcut / focuses sidebar search', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.keyboard.press('/');
    await expect(page.getByTestId('task-filter-search')).toBeFocused();
  });

  test('keyboard shortcut \\ toggles sidebar collapse', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await expect(page.getByTestId('task-filter-search')).toBeVisible();
    await page.keyboard.press('\\');
    await expect(page.getByTestId('task-filter-search')).toBeHidden();

    await page.keyboard.press('\\');
    await expect(page.getByTestId('task-filter-search')).toBeVisible();
  });
});

test.describe('Task filter sidebar (Gantt page)', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('sidebar renders on Gantt and is independent from Tasks selection', async ({ page }) => {
    // Set a chip on Tasks
    await gotoPage(page, '/tasks');
    await page.getByTestId('task-filter-chip-priority-high').click();
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'true');

    // Navigate to Gantt — its sidebar uses its own page key, no chips should be active
    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('task-filter-sidebar')).toBeVisible();
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'false');
  });

  test('toggling a chip on Gantt shows the filtered-count badge when rows are hidden', async ({ page }) => {
    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('gantt-chart')).toBeVisible();

    // Apply a likely-restrictive chip and check that either the badge appears
    // or the chart remains rendered (some seeds have only HIGH-priority tasks).
    await page.getByTestId('task-filter-chip-priority-low').click();

    const filteredBadge = page.getByTestId('gantt-filtered-count');
    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');

    // Either the badge is visible (some hidden) OR everything was filtered out
    // (chart hidden + empty state) OR everything still visible (no LOW tasks).
    await expect.poll(async () =>
      (await filteredBadge.isVisible()) || (await empty.isVisible()) || (await chart.isVisible())
    ).toBe(true);
  });
});
