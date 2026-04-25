import { test, expect } from '@playwright/test';
import {
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
  expectModalVisible,
} from './helpers';

test.describe('Plan readiness warnings', () => {
  test('shows banner when tasks exist — either info or warning', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    const tableVisible = await table.isVisible().catch(() => false);
    if (!tableVisible) {
      // No tasks exist — banner should not be visible (empty state page shown)
      await expect(page.getByTestId('task-warnings-banner')).not.toBeVisible();
      return;
    }

    const rows = await table.locator('tbody tr').count();
    if (rows > 0) {
      // Tasks exist — banner should be visible (either info or warning)
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).toBeVisible();
    }
  });

  test('resource gap warning appears for task with uncovered role', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('GapTest');

    // Add a task that requires a UX role (no UX resources in test DB)
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

    // Find the new task row
    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Resource gap warning should appear immediately (even before scheduler)
    const resourceGapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(resourceGapWarning).toBeVisible({ timeout: 5_000 });

    // Tooltip should mention UX
    const title = await resourceGapWarning.getAttribute('title');
    expect(title).toContain('UX');

    // Banner should mention resource gap
    const banner = page.getByTestId('task-warnings-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText('no matching active resources');

    // Clean up — delete the test task
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('unscheduled warning appears after scheduler run for tasks without planned start', async ({
    page,
  }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run the scheduler
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // After scheduler, check if any tasks remain unscheduled
    const unscheduledWarnings = page.locator('[data-testid^="unscheduled-warning-"]');
    const count = await unscheduledWarnings.count();

    if (count > 0) {
      // Verify the warning icon has the expected tooltip
      const firstWarning = unscheduledWarnings.first();
      const title = await firstWarning.getAttribute('title');
      expect(title).toContain('not scheduled');

      // Banner should mention unscheduled
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).toBeVisible();
      await expect(banner).toContainText('unscheduled');
    }
  });

  test('no warnings banner when all tasks are healthy after scheduler', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler
    await page.getByTestId('tasks-refresh').click();
    await page.waitForTimeout(3000);

    // Check if there are any warnings at all
    const unscheduledCount = await page
      .locator('[data-testid^="unscheduled-warning-"]')
      .count();
    const gapCount = await page.locator('[data-testid^="resource-gap-warning-"]').count();

    if (unscheduledCount === 0 && gapCount === 0) {
      // No issues — banner should be hidden (scheduler has run, no problems)
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).not.toBeVisible();
    }
  });

  test('resource gap tooltip lists specific uncovered role names', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const gapWarnings = page.locator('[data-testid^="resource-gap-warning-"]');
    const count = await gapWarnings.count();

    if (count > 0) {
      const firstWarning = gapWarnings.first();
      const title = await firstWarning.getAttribute('title');
      expect(title).toMatch(/No active resource for role\(s\):/);
      // Title should list at least one role name
      const rolePart = title!.split(':')[1].trim();
      expect(rolePart.length).toBeGreaterThan(0);
    }
  });
});
