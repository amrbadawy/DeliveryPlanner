import { test, expect, type Page } from '@playwright/test';
import { expectModalVisible, fillInputByTestId, gotoPage, runSchedulerFromDashboard, uniqueSuffix, waitForTableRows } from './helpers';

test.describe('Task Details + Notes', () => {
  test('task details page loads for a valid task', async ({ page }) => {
    // Navigate to tasks and get a task ID
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Click the first task's link to navigate to details
    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();

    // Should be on the task details page
    await expect(page.getByTestId('task-details-card')).toBeVisible();
    await expect(page.getByTestId('td-service-name')).toBeVisible();
    await expect(page.getByTestId('td-dev-estimation')).toBeVisible();
    await expect(page.getByTestId('td-priority')).toBeVisible();

    // Effort breakdown table should be visible with at least one row
    const effortTable = page.getByTestId('td-effort-breakdown');
    await expect(effortTable).toBeVisible();
    const effortRows = effortTable.locator('tbody tr');
    expect(await effortRows.count()).toBeGreaterThan(0);
  });

  test('add and delete a note', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Navigate to first task's details
    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Wait for notes section to load
    const notesSection = page.getByTestId('task-notes-section');
    await expect(notesSection).toBeVisible();

    const noteText = uniqueSuffix('E2E Note');
    const noteInput = page.getByTestId('task-note-input');
    const addBtn = page.getByTestId('task-note-add');

    // Add a note
    await noteInput.fill(noteText);
    await addBtn.click();

    // Note should appear (use exact text to avoid matching parent divs)
    await expect(notesSection.getByText(noteText, { exact: true })).toBeVisible({ timeout: 10_000 });

    // Delete the note — find the note row that contains our text, then click its delete button
    const noteRow = notesSection.locator('[data-testid^="task-note-"]').filter({ hasText: noteText });
    const deleteBtn = noteRow.locator('button[data-testid^="task-note-delete-"]');
    await deleteBtn.click();

    // Note should be gone
    await expect(notesSection.getByText(noteText, { exact: true })).toBeHidden({ timeout: 10_000 });
  });

  test('add note via Enter key', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    const notesSection = page.getByTestId('task-notes-section');
    await expect(notesSection).toBeVisible();

    const noteText = uniqueSuffix('E2E Enter Note');
    const noteInput = page.getByTestId('task-note-input');

    await noteInput.fill(noteText);
    // Blur to ensure Blazor @bind (change event) processes via SignalR, then re-focus and press Enter
    await noteInput.blur();
    await page.waitForTimeout(1000);
    await noteInput.focus();
    await noteInput.press('Enter');

    await expect(notesSection.getByText(noteText, { exact: true })).toBeVisible({ timeout: 10_000 });
  });

  test('back button navigates to tasks page', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Click back
    await page.getByTestId('task-details-back-bottom').click();
    await expect(page.getByTestId('tasks-table')).toBeVisible();
  });

  test('assigned resources section is visible', async ({ page }) => {
    await runSchedulerFromDashboard(page);

    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Assigned resources heading should be visible
    await expect(page.getByRole('heading', { name: /Assigned Resources/ })).toBeVisible();
  });

  test('task detail warnings card shows for task with resource gap', async ({ page }) => {
    // Create a task with UX role to guarantee a resource gap (no UX resources in seed data)
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('DetailGap');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '3');
    await fillInputByTestId(page, 'effort-days-QA', '1');
    await page.getByTestId('effort-add-role-select').selectOption('UX');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UX', '2');
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Navigate to the detail page
    const taskLink = newRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Task detail warning card should be visible with resource gap info
    const warnings = page.getByTestId('task-detail-warnings');
    await expect(warnings).toBeVisible();
    await expect(page.getByTestId('task-detail-resource-gap-warning')).toBeVisible();
    await expect(page.getByTestId('task-detail-resource-gap-warning')).toContainText('Resource gap');
    await expect(page.getByTestId('task-detail-resource-gap-warning')).toContainText('UX');

    // Clean up — go back and delete
    await page.getByTestId('task-details-back-bottom').click();
    await expect(page.getByTestId('tasks-table')).toBeVisible();
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('task detail shows unscheduled warning for task without planned dates', async ({ page }) => {
    // Create a task with only an unresolvable role so it stays unscheduled after scheduling
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('DetailUnsched');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '0');
    await fillInputByTestId(page, 'effort-days-QA', '0');
    await page.getByTestId('effort-add-role-select').selectOption('UI');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UI', '5');
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Run scheduler — task should remain unscheduled
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // Navigate to the detail page
    const taskLink = newRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Unscheduled warning should be visible on the detail page
    const warnings = page.getByTestId('task-detail-warnings');
    await expect(warnings).toBeVisible();
    await expect(page.getByTestId('task-detail-unscheduled-warning')).toBeVisible();
    await expect(page.getByTestId('task-detail-unscheduled-warning')).toContainText('Unscheduled');
    await expect(page.getByTestId('task-detail-unscheduled-warning')).toContainText('no planned dates');

    // Clean up
    await page.getByTestId('task-details-back-bottom').click();
    await expect(page.getByTestId('tasks-table')).toBeVisible();
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('task detail shows both unscheduled and resource gap warnings simultaneously', async ({ page }) => {
    // Create a task with only an unresolvable role — after scheduling it will be
    // both unscheduled AND have a resource gap (UI role has no matching resource)
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('BothWarn');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '0');
    await fillInputByTestId(page, 'effort-days-QA', '0');
    await page.getByTestId('effort-add-role-select').selectOption('UI');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UI', '5');
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Run scheduler
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // Navigate to detail page
    const taskLink = newRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // Both warning types should be visible simultaneously
    const warnings = page.getByTestId('task-detail-warnings');
    await expect(warnings).toBeVisible();
    await expect(page.getByTestId('task-detail-unscheduled-warning')).toBeVisible();
    await expect(page.getByTestId('task-detail-resource-gap-warning')).toBeVisible();

    // Clean up
    await page.getByTestId('task-details-back-bottom').click();
    await expect(page.getByTestId('tasks-table')).toBeVisible();
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('no warnings shown for a healthy scheduled task', async ({ page }) => {
    // Create a task with only DEV+QA roles (both have matching resources in seed data)
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('Healthy');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '3');
    await fillInputByTestId(page, 'effort-days-QA', '1');
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Run scheduler so the task gets scheduled
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // Navigate to the detail page
    const taskLink = newRow.locator('a').first();
    await taskLink.click();
    await expect(page.getByTestId('task-details-card')).toBeVisible();

    // No warning card should be present
    await expect(page.getByTestId('task-detail-warnings')).toBeHidden();

    // Clean up
    await page.getByTestId('task-details-back-bottom').click();
    await expect(page.getByTestId('tasks-table')).toBeVisible();
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('edit button opens existing tasks edit modal for same task', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskId = (await firstRow.locator('td').nth(0).innerText()).trim();
    await firstRow.locator('a').first().click();

    await expect(page.getByTestId('task-details-card')).toBeVisible();
    await page.getByTestId('task-details-edit').click();

    await expect(page).toHaveURL(new RegExp(`/tasks\\?editTaskId=${taskId}$`));
    await expect(page.getByTestId('tasks-modal')).toBeVisible();
    await expect(page.getByTestId('tasks-task-id')).toHaveValue(taskId);
  });

  test('assignment timeline shows role-colored allocation percent badges', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/tasks');

    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);
    await table.locator('tbody tr').first().locator('a').first().click();

    await expect(page.getByTestId('task-timeline-table')).toBeVisible();

    const badges = page.locator('[data-testid^="timeline-res-"]');
    const badgeCount = await badges.count();
    if (badgeCount === 0) {
      test.skip(true, 'No assigned resources in timeline for selected task');
    }

    const firstBadge = badges.first();
    await expect(firstBadge).toContainText('%');
    await expect(firstBadge).toContainText('·');
    await expect(firstBadge).toHaveClass(/role-badge-/);
  });

  test('edit effort breakdown from details persists after refresh', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskLink = firstRow.locator('a').first();
    await taskLink.click();

    await expect(page.getByTestId('task-details-card')).toBeVisible();
    await page.getByTestId('task-effort-edit').click();
    await expect(page.getByTestId('task-effort-modal')).toBeVisible();

    await fillInputByTestId(page, 'effort-edit-days-DEV', '12.5');
    await page.getByTestId('task-effort-save').click();
    await expect(page.getByTestId('task-effort-modal')).toBeHidden();

    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText('12.5');

    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText('12.5');
  });
});

// ── Helper shared by inline-edit tests ───────────────────────────────────────
async function navigateToFirstTaskDetail(page: Page) {
  await gotoPage(page, '/tasks');
  const table = page.getByTestId('tasks-table');
  await waitForTableRows(table);
  await table.locator('tbody tr').first().locator('a').first().click();
  await expect(page.getByTestId('task-details-card')).toBeVisible();
  await expect(page.getByTestId('td-effort-breakdown')).toBeVisible();
}

// ── Inline per-row effort breakdown editing ───────────────────────────────────
test.describe('Effort Breakdown — inline editing', () => {
  test('inline edit button appears on each effort row', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await expect(page.getByTestId('effort-inline-edit-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-edit-QA')).toBeVisible();
  });

  test('clicking edit on a row reveals inline inputs and hides the pencil button', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-DEV').click();

    await expect(page.getByTestId('effort-inline-days-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-maxfte-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-seniority-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-cancel-DEV')).toBeVisible();
    // Pencil button is gone while row is in edit mode
    await expect(page.getByTestId('effort-inline-edit-DEV')).toBeHidden();
  });

  test('primary role (lowest sort order) does not show overlap input; non-primary does', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Discover which role has the lowest SortOrder from the rendered table
    const effortTable = page.getByTestId('td-effort-breakdown');
    const firstRowTestId = await effortTable.locator('tbody tr').first().getAttribute('data-testid');
    const firstRole = (firstRowTestId ?? 'td-effort-row-DEV').replace('td-effort-row-', '');
    const lastRowTestId = await effortTable.locator('tbody tr').last().getAttribute('data-testid');
    const lastRole = (lastRowTestId ?? 'td-effort-row-QA').replace('td-effort-row-', '');

    // First (primary) role — overlap must NOT be rendered
    await page.getByTestId(`effort-inline-edit-${firstRole}`).click();
    await expect(page.getByTestId(`effort-inline-overlap-${firstRole}`)).toBeHidden();
    await page.getByTestId(`effort-inline-cancel-${firstRole}`).click();

    // Last role (non-primary when more than one entry exists) — overlap MUST be rendered
    if (firstRole !== lastRole) {
      await page.getByTestId(`effort-inline-edit-${lastRole}`).click();
      await expect(page.getByTestId(`effort-inline-overlap-${lastRole}`)).toBeVisible();
      await page.getByTestId(`effort-inline-cancel-${lastRole}`).click();
    }
  });

  test('happy path: inline edit DEV days saves and persists after reload', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', '8.5');
    await page.getByTestId('effort-inline-save-DEV').click();

    // Inputs should be gone, row back to read-only
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText('8.5');

    // Persist after reload
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText('8.5');
  });

  test('happy path: inline edit QA days saves and persists after reload', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-QA').click();
    await fillInputByTestId(page, 'effort-inline-days-QA', '4.5');
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-save-QA')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(1)).toContainText('4.5');

    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(1)).toContainText('4.5');
  });

  test('happy path: inline edit changes seniority', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-seniority-DEV', 'Senior');
    await page.getByTestId('effort-inline-save-DEV').click();

    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    // Seniority is the 5th column (index 4) in the read-only row
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(4)).toContainText('Senior');

    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(4)).toContainText('Senior');
  });

  test('happy path: inline edit changes MaxFte', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-maxfte-DEV', '2');
    await page.getByTestId('effort-inline-save-DEV').click();

    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    // MaxFte is the 3rd column (index 2) in the read-only row
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(2)).toContainText('2');
  });

  test('happy path: inline edit overlap on non-primary role (QA)', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-QA').click();
    // QA is not the primary role so overlap is editable
    await expect(page.getByTestId('effort-inline-overlap-QA')).toBeVisible();
    await fillInputByTestId(page, 'effort-inline-overlap-QA', '20');
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-save-QA')).toBeHidden();
    // Overlap is the 4th column (index 3) in the read-only row
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(3)).toContainText('20');
  });

  test('toast notification appears on successful inline save', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-QA').click();
    await fillInputByTestId(page, 'effort-inline-days-QA', '3');
    await page.getByTestId('effort-inline-save-QA').click();
    await expect(page.getByTestId('task-details-toast')).toBeVisible({ timeout: 10_000 });
  });

  test('cancel restores original value without saving', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Capture current displayed days before editing
    const originalDays = (await page.getByTestId('td-effort-row-DEV').locator('td').nth(1).innerText()).trim();

    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', '999');
    await page.getByTestId('effort-inline-cancel-DEV').click();

    // Edit inputs gone
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    // Original value still shown
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText(originalDays);
  });

  test('only one row can be in edit mode at a time', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Open DEV row
    await page.getByTestId('effort-inline-edit-DEV').click();
    await expect(page.getByTestId('effort-inline-days-DEV')).toBeVisible();

    // Open QA row — DEV row should revert to read-only
    await page.getByTestId('effort-inline-edit-QA').click();
    await expect(page.getByTestId('effort-inline-days-DEV')).toBeHidden();
    await expect(page.getByTestId('effort-inline-days-QA')).toBeVisible();

    await page.getByTestId('effort-inline-cancel-QA').click();
  });

  test('opening the bulk-edit modal cancels active inline edit', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Start inline editing DEV
    await page.getByTestId('effort-inline-edit-DEV').click();
    await expect(page.getByTestId('effort-inline-days-DEV')).toBeVisible();

    // Open the "Edit Effort Breakdown" modal
    await page.getByTestId('task-effort-edit').click();
    await expect(page.getByTestId('task-effort-modal')).toBeVisible();

    // Inline edit should be cancelled automatically
    await expect(page.getByTestId('effort-inline-days-DEV')).toBeHidden();

    await page.getByTestId('task-effort-cancel').click();
  });

  test('validation error: days = 0 shows inline error and keeps row open', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', '0');
    await page.getByTestId('effort-inline-save-DEV').click();

    await expect(page.getByTestId('effort-inline-error-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-error-DEV')).toContainText('greater than zero');
    // Row stays open
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeVisible();

    await page.getByTestId('effort-inline-cancel-DEV').click();
  });

  test('validation error: MaxFte = 0 shows inline error', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-maxfte-DEV', '0');
    await page.getByTestId('effort-inline-save-DEV').click();

    await expect(page.getByTestId('effort-inline-error-DEV')).toBeVisible();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeVisible();

    await page.getByTestId('effort-inline-cancel-DEV').click();
  });

  test('validation error: overlap > 100 on non-primary role shows inline error', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-QA').click();
    // Force an out-of-range overlap value via JS (bypasses the browser max=100 constraint)
    await page.getByTestId('effort-inline-overlap-QA').evaluate((el: HTMLInputElement) => {
      el.value = '150';
      el.dispatchEvent(new Event('change', { bubbles: true }));
    });
    await page.waitForTimeout(150);
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-error-QA')).toBeVisible();
    await expect(page.getByTestId('effort-inline-error-QA')).toContainText('0 and 100');

    await page.getByTestId('effort-inline-cancel-QA').click();
  });

  test('cancelling inline edit clears validation error', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Trigger a validation error
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', '0');
    await page.getByTestId('effort-inline-save-DEV').click();
    await expect(page.getByTestId('effort-inline-error-DEV')).toBeVisible();

    // Cancel — error row and inputs should disappear
    await page.getByTestId('effort-inline-cancel-DEV').click();
    await expect(page.getByTestId('effort-inline-error-DEV')).toBeHidden();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
  });

  test('opening a second row clears validation error from first row', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Trigger error on DEV row
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', '0');
    await page.getByTestId('effort-inline-save-DEV').click();
    await expect(page.getByTestId('effort-inline-error-DEV')).toBeVisible();

    // Open QA row — DEV error should be gone
    await page.getByTestId('effort-inline-edit-QA').click();
    await expect(page.getByTestId('effort-inline-error-DEV')).toBeHidden();
    await expect(page.getByTestId('effort-inline-days-QA')).toBeVisible();

    await page.getByTestId('effort-inline-cancel-QA').click();
  });

  test('seniority can be reset from a set value back to Any', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // First set seniority to Senior
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-seniority-DEV', 'Senior');
    await page.getByTestId('effort-inline-save-DEV').click();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(4)).toContainText('Senior');

    // Now reset to Any (empty value)
    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-seniority-DEV', '');
    await page.getByTestId('effort-inline-save-DEV').click();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(4)).toContainText('Any');

    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(4)).toContainText('Any');
  });

  test('TotalEstimationDays in header card updates after inline save', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Read current total
    const totalBefore = parseFloat((await page.getByTestId('td-dev-estimation').innerText()).trim());

    // Read current DEV days
    const devDaysBefore = parseFloat((await page.getByTestId('td-effort-row-DEV').locator('td').nth(1).innerText()).trim());

    // Set DEV days to a known different value
    const newDays = devDaysBefore === 7 ? 9 : 7;
    const expectedTotal = totalBefore - devDaysBefore + newDays;

    await page.getByTestId('effort-inline-edit-DEV').click();
    await fillInputByTestId(page, 'effort-inline-days-DEV', String(newDays));
    await page.getByTestId('effort-inline-save-DEV').click();
    await expect(page.getByTestId('effort-inline-save-DEV')).toBeHidden();

    // Total estimation should reflect the change
    await expect(page.getByTestId('td-dev-estimation')).toContainText(String(expectedTotal));
  });

  test('validation error: negative overlap on non-primary role shows inline error', async ({ page }) => {
    await navigateToFirstTaskDetail(page);
    await page.getByTestId('effort-inline-edit-QA').click();
    // Force a negative overlap via JS (bypasses browser min=0 constraint)
    await page.getByTestId('effort-inline-overlap-QA').evaluate((el: HTMLInputElement) => {
      el.value = '-10';
      el.dispatchEvent(new Event('change', { bubbles: true }));
    });
    await page.waitForTimeout(150);
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-error-QA')).toBeVisible();
    await expect(page.getByTestId('effort-inline-error-QA')).toContainText('0 and 100');

    await page.getByTestId('effort-inline-cancel-QA').click();
  });

  test('bulk modal save still works correctly after a prior inline edit', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    // Inline edit: change QA days
    await page.getByTestId('effort-inline-edit-QA').click();
    await fillInputByTestId(page, 'effort-inline-days-QA', '5');
    await page.getByTestId('effort-inline-save-QA').click();
    await expect(page.getByTestId('effort-inline-save-QA')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(1)).toContainText('5');

    // Now open bulk modal and change DEV days
    await page.getByTestId('task-effort-edit').click();
    await expect(page.getByTestId('task-effort-modal')).toBeVisible();
    await fillInputByTestId(page, 'effort-edit-days-DEV', '11');
    await page.getByTestId('task-effort-save').click();
    await expect(page.getByTestId('task-effort-modal')).toBeHidden();

    // Both changes should be reflected
    await expect(page.getByTestId('td-effort-row-DEV').locator('td').nth(1)).toContainText('11');
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(1)).toContainText('5');
  });

  test('overlap boundary: 0 saves correctly on non-primary role', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-QA').click();
    await fillInputByTestId(page, 'effort-inline-overlap-QA', '0');
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-save-QA')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(3)).toContainText('0');
  });

  test('overlap boundary: 100 saves correctly on non-primary role', async ({ page }) => {
    await navigateToFirstTaskDetail(page);

    await page.getByTestId('effort-inline-edit-QA').click();
    await fillInputByTestId(page, 'effort-inline-overlap-QA', '100');
    await page.getByTestId('effort-inline-save-QA').click();

    await expect(page.getByTestId('effort-inline-save-QA')).toBeHidden();
    await expect(page.getByTestId('td-effort-row-QA').locator('td').nth(3)).toContainText('100');
  });
});
