import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, uniqueSuffix, waitForTableRows } from './helpers';

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

    // Note should appear
    await expect(notesSection.locator('div', { hasText: noteText })).toBeVisible({ timeout: 10_000 });

    // Delete the note
    const noteElement = notesSection.locator(`div:has-text("${noteText}")`).first();
    const deleteBtn = noteElement.locator('button[data-testid^="task-note-delete-"]');
    await deleteBtn.click();

    // Note should be gone
    await expect(notesSection.locator(`div:has-text("${noteText}")`).first()).toBeHidden({ timeout: 10_000 });
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
    await noteInput.press('Enter');

    await expect(notesSection.locator('div', { hasText: noteText })).toBeVisible({ timeout: 10_000 });
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
});
