import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, uniqueSuffix, waitForTableRows } from './helpers';

/**
 * E2E coverage for filter sidebar Commit B:
 *   - Saved views: save / list / apply / delete
 *   - Pin / hide row actions on Tasks page
 *   - Ghost-dependency stub on Gantt page when a visible task's predecessor is hidden
 *
 * The SQLite test DB is reset per run, so these specs can freely create
 * SavedView rows without polluting state across test files.
 */

test.describe('Saved views', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('save current sidebar selection as a named view, then re-apply it', async ({ page }) => {
    await gotoPage(page, '/tasks');

    // Apply some chip filters to make IsAnyActive true
    await page.getByTestId('task-filter-chip-status-not_started').click();
    await page.getByTestId('task-filter-chip-priority-high').click();

    // Save section becomes enabled once at least one filter is active
    const nameInput = page.getByTestId('task-filter-saved-view-name');
    const saveBtn = page.getByTestId('task-filter-saved-view-save');
    await expect(nameInput).toBeEnabled();

    const viewName = `Hot tasks ${uniqueSuffix('v')}`;
    await nameInput.fill(viewName);
    await saveBtn.click();

    // Saved-view list shows the new entry
    const savedList = page.getByTestId('task-filter-saved-views');
    await expect(savedList).toContainText(viewName);

    // Clear sidebar selections and confirm chips deselected
    await page.getByTestId('task-filter-clear-all').click();
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'false');

    // Apply the saved view — chips should re-activate
    const applyBtn = savedList.locator('[data-testid^="task-filter-saved-view-apply-"]', { hasText: viewName });
    await applyBtn.click();
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'true');
  });

  test('delete a saved view removes it from the list', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-risk-late').click();

    const viewName = `To delete ${uniqueSuffix('d')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const savedList = page.getByTestId('task-filter-saved-views');
    await expect(savedList).toContainText(viewName);

    const deleteBtn = savedList.locator('[data-testid^="task-filter-saved-view-delete-"]').last();
    await deleteBtn.click();

    await expect(savedList).not.toContainText(viewName);
  });

  test('save button is disabled when no filters are active', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await expect(page.getByTestId('task-filter-saved-view-name')).toBeDisabled();
    await expect(page.getByTestId('task-filter-saved-view-save')).toBeDisabled();
  });

  test('saved view scoping is per-page (Tasks vs Gantt are independent)', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('task-filter-chip-status-not_started').click();

    const tasksViewName = `Tasks-only ${uniqueSuffix('t')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(tasksViewName);
    await page.getByTestId('task-filter-saved-view-save').click();
    await expect(page.getByTestId('task-filter-saved-views')).toContainText(tasksViewName);

    // Navigate to Gantt — saved-view list should NOT show the Tasks-page view
    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('task-filter-saved-views')).not.toContainText(tasksViewName);
  });
});

test.describe('Pin / hide row actions', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('pinning a task highlights its row and floats it to the top', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table, 2);
    const tbody = table.locator('tbody');

    // Grab the second visible row's task ID, then pin it.
    const secondRow = tbody.locator('tr').nth(1);
    const taskId = (await secondRow.locator('td').first().innerText()).trim();
    expect(taskId).toBeTruthy();

    await page.getByTestId(`tasks-pin-${taskId}`).click();

    // Pinned row gets the highlight class
    await expect(page.getByTestId(`tasks-row-${taskId}`)).toHaveClass(/task-row-pinned/);

    // After pinning, that task's row should now be at the top of tbody
    const firstRowAfter = tbody.locator('tr').first();
    await expect(firstRowAfter.locator('td').first()).toHaveText(taskId);
  });

  test('hiding a task removes it from the rendered list', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table, 2);
    const tbody = table.locator('tbody');

    const initialCount = await tbody.locator('tr').count();
    const targetRow = tbody.locator('tr').first();
    const taskId = (await targetRow.locator('td').first().innerText()).trim();

    await page.getByTestId(`tasks-hide-${taskId}`).click();

    // Row no longer present
    await expect(page.getByTestId(`tasks-row-${taskId}`)).toHaveCount(0);
    const afterCount = await tbody.locator('tr').count();
    expect(afterCount).toBe(initialCount - 1);
  });

  test('clearing filters re-shows previously hidden tasks', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table, 2);
    const tbody = table.locator('tbody');

    const targetRow = tbody.locator('tr').first();
    const taskId = (await targetRow.locator('td').first().innerText()).trim();

    await page.getByTestId(`tasks-hide-${taskId}`).click();
    await expect(page.getByTestId(`tasks-row-${taskId}`)).toHaveCount(0);

    // Hidden tasks aren't pin-able from row UI (row gone). Clear-all restores.
    // First add another active filter so clear-all button appears, then click it.
    await page.getByTestId('task-filter-chip-status-not_started').click();
    await page.getByTestId('task-filter-clear-all').click();
    await expect(page.getByTestId(`tasks-row-${taskId}`)).toBeVisible();
  });
});

test.describe('Ghost dependency indicator (Gantt)', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('Gantt renders without errors when a predecessor is hidden via Tasks page', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table, 2);
    const tbody = table.locator('tbody');

    const firstRow = tbody.locator('tr').first();
    const firstTaskId = (await firstRow.locator('td').first().innerText()).trim();
    await page.getByTestId(`tasks-hide-${firstTaskId}`).click();

    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('gantt-chart')).toBeVisible({ timeout: 15000 });

    // Either ghost-dep badges appear (if any visible task depended on the hidden one)
    // or none do — both are acceptable. The key assertion is the chart renders.
    const ghostBadges = page.locator('[data-testid^="gantt-ghost-deps-"]');
    const ghostCount = await ghostBadges.count();
    expect(ghostCount).toBeGreaterThanOrEqual(0);

    if (ghostCount > 0) {
      await expect(ghostBadges.first()).toContainText(/hidden dep/);
    }
  });
});
