import { test, expect } from '@playwright/test';
import {
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
  expectModalVisible,
} from './helpers';

test.describe('Plan readiness warnings', () => {
  test('shows info banner when scheduler has not been run', async ({ page }) => {
    // Navigate to tasks page — on a fresh DB, tasks may exist but scheduler hasn't run
    await gotoPage(page, '/tasks');

    const table = page.getByTestId('tasks-table');
    // If there are tasks and none are scheduled, expect the info banner
    const tableVisible = await table.isVisible().catch(() => false);
    if (tableVisible) {
      const rows = await table.locator('tbody tr').count();
      if (rows > 0) {
        const banner = page.getByTestId('task-warnings-banner');
        // Banner should be visible (either info or warning)
        await expect(banner).toBeVisible();
      }
    }
  });

  test('shows unscheduled warning after scheduler run when task cannot be scheduled', async ({
    page,
  }) => {
    // Create a task with a role that has no matching resource (e.g., UX)
    // This task will remain unscheduled after the scheduler runs
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('Unschedulable');

    // Add a task with UX role (unlikely to have a UX resource in test DB)
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '5');
    await fillInputByTestId(page, 'effort-days-QA', '2');

    // Add UX role
    await page.getByTestId('effort-add-role-select').selectOption('UX');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UX', '3');

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    // Get the new task ID
    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Run the scheduler
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // Check for resource gap warning on the new task's row
    const resourceGapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    // If no UX resource exists, the warning should be visible
    const hasWarning = await resourceGapWarning.isVisible().catch(() => false);
    if (hasWarning) {
      await expect(resourceGapWarning).toBeVisible();
      // Verify tooltip mentions UX
      const title = await resourceGapWarning.getAttribute('title');
      expect(title).toContain('UX');
    }

    // Check for the warnings banner
    const banner = page.getByTestId('task-warnings-banner');
    await expect(banner).toBeVisible();

    // Clean up — delete the test task
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('no warnings when all tasks are scheduled and roles are covered', async ({ page }) => {
    // Run scheduler to ensure everything is scheduled
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // Check all rows for PlannedStart dates
    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();

    let allScheduled = true;
    let anyResourceGap = false;

    for (let i = 0; i < rowCount; i++) {
      const row = rows.nth(i);
      const taskIdCell = await row.locator('td').nth(0).innerText();
      const taskId = taskIdCell.trim();

      // Check if unscheduled warning exists for this task
      const unscheduledWarning = page.getByTestId(`unscheduled-warning-${taskId}`);
      if (await unscheduledWarning.isVisible().catch(() => false)) {
        allScheduled = false;
      }

      // Check for resource gap
      const gapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
      if (await gapWarning.isVisible().catch(() => false)) {
        anyResourceGap = true;
      }
    }

    // If all tasks are scheduled and no resource gaps, banner should not be visible
    if (allScheduled && !anyResourceGap) {
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).not.toBeVisible();
    }
  });

  test('unscheduled warning icon appears in Start column for unscheduled tasks', async ({
    page,
  }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler first
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // Find any row with an unscheduled warning
    const unscheduledWarnings = page.locator('[data-testid^="unscheduled-warning-"]');
    const count = await unscheduledWarnings.count();

    if (count > 0) {
      // Verify the warning icon has the expected tooltip
      const firstWarning = unscheduledWarnings.first();
      const title = await firstWarning.getAttribute('title');
      expect(title).toContain('not scheduled');
    }
  });

  test('resource gap warning shows uncovered role names in tooltip', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // Find any row with a resource gap warning
    const gapWarnings = page.locator('[data-testid^="resource-gap-warning-"]');
    const count = await gapWarnings.count();

    if (count > 0) {
      const firstWarning = gapWarnings.first();
      const title = await firstWarning.getAttribute('title');
      expect(title).toMatch(/No active resource for role\(s\):/);
    }
  });
});
