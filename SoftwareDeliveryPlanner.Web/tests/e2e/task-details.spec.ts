import { test, expect } from '@playwright/test';
import { fillInputByTestId, gotoPage, runSchedulerFromDashboard, uniqueSuffix, waitForTableRows } from './helpers';

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
