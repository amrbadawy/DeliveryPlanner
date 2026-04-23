import { test, expect } from '@playwright/test';
import {
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  runSchedulerFromDashboard,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';

test.describe('Task dependencies', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/tasks');
    await waitForTableRows(page.getByTestId('tasks-table'));
  });

  test('dependency checkbox list is visible in add modal', async ({ page }) => {
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    const depsContainer = page.getByTestId('tasks-depends-on');
    await expect(depsContainer).toBeVisible();

    // Should list existing tasks as checkboxes
    const checkboxCount = await depsContainer.locator('input[type="checkbox"]').count();
    expect(checkboxCount).toBeGreaterThan(0);
  });

  test('add task with dependency and see badge in Deps column', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    // Get the first task's ID to use as a dependency
    const firstRow = table.locator('tbody tr').first();
    const depTaskId = (await firstRow.locator('td').nth(0).innerText()).trim();

    // Add a new task with a dependency
    const serviceName = uniqueSuffix('E2E DepTask');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '3');
    await fillInputByTestId(page, 'tasks-priority', '5');

    // Check the dependency checkbox
    const depCheckbox = page.getByTestId(`dep-check-${depTaskId}`);
    await depCheckbox.check();

    // Verify the dependency detail controls appear (type selector, lag input)
    await expect(page.getByTestId(`dep-type-${depTaskId}`)).toBeVisible();

    // Save
    const taskId = (await page.getByTestId('tasks-task-id').inputValue()).trim();
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    // Verify the new task row has a dependency badge
    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    await expect(newRow.locator('.badge', { hasText: depTaskId })).toBeVisible();

    // Cleanup: delete the test task
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId(`tasks-row-${taskId}`)).toHaveCount(0);
  });

  test('edit task to add dependency and verify persistence', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    // Find a task that currently has no dependencies
    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();
    let targetTaskId = '';

    for (let i = 0; i < rowCount; i++) {
      const row = rows.nth(i);
      const depsCell = row.locator('td').nth(10);
      const badges = await depsCell.locator('.badge').count();
      if (badges === 0) {
        targetTaskId = (await row.locator('td').nth(0).innerText()).trim();
        break;
      }
    }

    // Skip if all tasks have dependencies (unlikely with seed data)
    if (!targetTaskId) {
      test.skip();
      return;
    }

    // Get another task ID to add as dependency
    const allTaskIds: string[] = [];
    for (let i = 0; i < rowCount; i++) {
      const id = (await rows.nth(i).locator('td').nth(0).innerText()).trim();
      if (id !== targetTaskId) {
        allTaskIds.push(id);
      }
    }
    const depId = allTaskIds[0];

    // Edit the task
    await page.getByTestId(`tasks-edit-${targetTaskId}`).click();
    await expectModalVisible(page, 'tasks-modal');

    // Check the dependency checkbox
    const depCheckbox = page.getByTestId(`dep-check-${depId}`);
    await depCheckbox.check();

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden({ timeout: 10_000 });

    // Verify badge appears
    const targetRow = table.locator(`[data-testid="tasks-row-${targetTaskId}"]`);
    await expect(targetRow.locator('.badge', { hasText: depId })).toBeVisible();

    // Edit again to remove the dependency (restore original state)
    await page.getByTestId(`tasks-edit-${targetTaskId}`).click();
    await expectModalVisible(page, 'tasks-modal');

    // Uncheck the dependency checkbox
    const depCheckbox2 = page.getByTestId(`dep-check-${depId}`);
    await depCheckbox2.uncheck();

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();
  });

  test('deps column shows multiple dependency badges', async ({ page }) => {
    const table = page.getByTestId('tasks-table');
    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(2);

    // Get two task IDs to use as dependencies
    const taskIds: string[] = [];
    for (let i = 0; i < Math.min(3, rowCount); i++) {
      taskIds.push((await rows.nth(i).locator('td').nth(0).innerText()).trim());
    }

    // Add a new task depending on the first two
    const serviceName = uniqueSuffix('E2E MultiDep');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '2');
    await fillInputByTestId(page, 'tasks-priority', '5');

    // Check two dependency checkboxes
    await page.getByTestId(`dep-check-${taskIds[0]}`).check();
    await page.getByTestId(`dep-check-${taskIds[1]}`).check();

    const newTaskId = (await page.getByTestId('tasks-task-id').inputValue()).trim();
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    // Verify the new row has two dependency badges
    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    await expect(newRow.locator('.badge', { hasText: taskIds[0] })).toBeVisible();
    await expect(newRow.locator('.badge', { hasText: taskIds[1] })).toBeVisible();

    // Cleanup
    await page.getByTestId(`tasks-delete-${newTaskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });
});
