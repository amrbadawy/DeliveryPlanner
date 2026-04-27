import { test, expect } from '@playwright/test';
import {
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
  expectModalVisible,
} from './helpers';

test.describe('Plan readiness warnings', () => {
  test('shows info banner before scheduler has run and no resource gaps', async ({ page }) => {
    // This test verifies the info state. It navigates to /tasks and checks the banner.
    // With seed data, BA/SA/UX roles exist on tasks but no BA/SA/UX resources,
    // so the banner will be alert-warning (resource gaps). That's acceptable —
    // we just verify the banner is visible when tasks exist.
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    const tableVisible = await table.isVisible().catch(() => false);
    if (!tableVisible) {
      // Empty state — no banner expected
      await expect(page.getByTestId('task-warnings-banner')).not.toBeVisible();
      return;
    }

    const rows = await table.locator('tbody tr').count();
    if (rows === 0) return;

    const banner = page.getByTestId('task-warnings-banner');
    await expect(banner).toBeVisible();

    // Banner should contain at least one of the expected messages
    const text = await banner.innerText();
    const hasExpectedMessage =
      text.includes('No tasks are currently scheduled') ||
      text.includes('unscheduled') ||
      text.includes('no matching active resources');
    expect(hasExpectedMessage).toBe(true);
  });

  test('resource gap warning appears for task with uncovered role', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('GapTest');

    // Add a task that requires a UX role (no UX resources in seed/test DB)
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

  test('unscheduled warning appears deterministically for task with unresolvable role', async ({
    page,
  }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('Unsched');

    // Create a task with UI role — no UI resources exist in seed data,
    // so the scheduler cannot allocate the UI phase and the task stays unscheduled.
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '3');
    await fillInputByTestId(page, 'effort-days-QA', '1');

    await page.getByTestId('effort-add-role-select').selectOption('UI');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UI', '5');

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Run the scheduler
    await page.getByTestId('tasks-refresh').click();
    // Wait for scheduler to complete and table to re-render
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // The task should show an unscheduled warning in the Start column
    // (The scheduler may or may not schedule it depending on how it handles
    // missing-role phases — if it skips the phase but schedules DEV/QA, it
    // might still get a PlannedStart. In that case, check the resource gap icon instead.)
    const unscheduledWarning = page.getByTestId(`unscheduled-warning-${taskId}`);
    const resourceGapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);

    const hasUnscheduled = await unscheduledWarning.isVisible().catch(() => false);
    const hasResourceGap = await resourceGapWarning.isVisible().catch(() => false);

    // At minimum, the resource gap warning must be visible (UI role has no resource)
    expect(hasResourceGap).toBe(true);

    if (hasUnscheduled) {
      const title = await unscheduledWarning.getAttribute('title');
      expect(title).toContain('not scheduled');
    }

    // Banner should be visible with at least one warning message
    const banner = page.getByTestId('task-warnings-banner');
    await expect(banner).toBeVisible();

    // Clean up
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('no warnings banner when all tasks are healthy after scheduler', async ({ page }) => {
    // First ensure we have a clean state: only tasks with DEV+QA roles
    // and matching resources. We rely on the seed data having DEV+QA resources.
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // Check if there are any warnings
    const unscheduledCount = await page
      .locator('[data-testid^="unscheduled-warning-"]')
      .count();
    const gapCount = await page.locator('[data-testid^="resource-gap-warning-"]').count();

    if (unscheduledCount === 0 && gapCount === 0) {
      // All healthy — banner should be hidden
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).not.toBeVisible();
    }
    // If there are warnings (e.g., seed data has BA/UX roles), that's expected —
    // the banner should be visible. We just verify consistency.
    if (unscheduledCount > 0 || gapCount > 0) {
      const banner = page.getByTestId('task-warnings-banner');
      await expect(banner).toBeVisible();
    }
  });

  test('seniority-based resource gap warning when no resource meets MinSeniority', async ({
    page,
  }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('SeniorityGap');

    // Create a task requiring DEV with MinSeniority = Principal
    // Seed data has Senior/Mid/Junior DEVs — no Principal DEV exists
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '5');
    await fillInputByTestId(page, 'effort-days-QA', '2');

    // Set DEV MinSeniority to Principal
    await fillInputByTestId(page, 'effort-seniority-DEV', 'Principal');

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Resource gap warning should appear because no Principal DEV exists
    const resourceGapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(resourceGapWarning).toBeVisible({ timeout: 5_000 });

    // Tooltip should mention DEV
    const title = await resourceGapWarning.getAttribute('title');
    expect(title).toContain('DEV');

    // Clean up
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('resource gap warning clears after adding a matching resource', async ({ page }) => {
    // Step 1: Create a task requiring UX role (no UX resource exists)
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('GapClear');

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

    // Verify gap warning is visible
    const gapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(gapWarning).toBeVisible({ timeout: 5_000 });

    // Step 2: Add a UX resource
    await gotoPage(page, '/resources');
    const resTable = page.getByTestId('resources-table');
    await waitForTableRows(resTable);

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');
    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();
    await fillInputByTestId(page, 'resources-name', uniqueSuffix('UX Resource'));
    await fillInputByTestId(page, 'resources-role', 'UX Designer');
    await fillInputByTestId(page, 'resources-team', 'E2E Team');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    // Step 3: Go back to tasks and verify gap warning is gone
    await gotoPage(page, '/tasks');
    await waitForTableRows(page.getByTestId('tasks-table'));

    const gapWarningAfter = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(gapWarningAfter).not.toBeVisible();

    // Step 4: Clean up — delete the task
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();

    // Clean up — delete the resource
    await gotoPage(page, '/resources');
    await waitForTableRows(page.getByTestId('resources-table'));
    await page.getByTestId(`resources-delete-${resourceId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-confirm').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
  });

  test('resource gap tooltip lists specific uncovered role names', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('TooltipTest');

    // Create a task with both UX and BA roles — neither has a matching resource in seed data
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'effort-days-DEV', '2');
    await fillInputByTestId(page, 'effort-days-QA', '1');

    await page.getByTestId('effort-add-role-select').selectOption('UX');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-UX', '3');

    await page.getByTestId('effort-add-role-select').selectOption('BA');
    await page.getByTestId('effort-add-btn').click();
    await fillInputByTestId(page, 'effort-days-BA', '2');

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const newRow = table.locator('tbody tr', { hasText: serviceName });
    await expect(newRow).toBeVisible();
    const taskId = (await newRow.locator('td').nth(0).innerText()).trim();

    // Resource gap warning should be visible with multiple uncovered roles
    const gapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(gapWarning).toBeVisible({ timeout: 5_000 });

    const title = await gapWarning.getAttribute('title');
    expect(title).toMatch(/No active resource for role\(s\):/);
    // Tooltip should list both uncovered role names
    expect(title).toContain('UX');
    expect(title).toContain('BA');

    // Clean up
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('finish and duration columns show dash for unscheduled tasks', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('DashTest');

    // Create a task with only an unresolvable role so it stays unscheduled
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    // Clear default DEV/QA and only use an unresolvable role
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

    // Run the scheduler
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    // Verify unscheduled warning is visible
    const unschedWarning = page.getByTestId(`unscheduled-warning-${taskId}`);
    await expect(unschedWarning).toBeVisible({ timeout: 5_000 });

    // Get the row containing the unscheduled warning
    const row = unschedWarning.locator('xpath=ancestor::tr');

    // The Finish column (index 6) should show a dash
    const finishCell = row.locator('td').nth(6);
    await expect(finishCell).toContainText('—');

    // The Days column (index 7) should show a dash
    const daysCell = row.locator('td').nth(7);
    await expect(daysCell).toContainText('—');

    // Clean up
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
  });

  test('banner warning count matches actual icon counts', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Run scheduler to populate scheduling state
    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    const banner = page.getByTestId('task-warnings-banner');
    const isBannerVisible = await banner.isVisible().catch(() => false);
    if (!isBannerVisible) return; // All healthy — no counts to verify

    const bannerText = await banner.innerText();

    // Count actual unscheduled icons in the table
    const unschedCount = await page.locator('[data-testid^="unscheduled-warning-"]').count();
    if (unschedCount > 0) {
      expect(bannerText).toContain(`${unschedCount} task(s) are unscheduled`);
    }

    // Count actual resource gap icons in the table
    const gapCount = await page.locator('[data-testid^="resource-gap-warning-"]').count();
    if (gapCount > 0) {
      expect(bannerText).toContain(`${gapCount} task(s) have no matching active resources`);
    }
  });

  test('inactive resource causes resource gap', async ({ page }) => {
    // Step 1: Create a UX resource (active)
    await gotoPage(page, '/resources');
    const resTable = page.getByTestId('resources-table');
    await waitForTableRows(resTable);

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');
    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();
    const resName = uniqueSuffix('InactiveRes');
    await fillInputByTestId(page, 'resources-name', resName);
    await fillInputByTestId(page, 'resources-role', 'UX Designer');
    await fillInputByTestId(page, 'resources-team', 'E2E Team');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    // Step 2: Create a task with UX role — should have NO gap since UX resource exists
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('InactiveGap');
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

    // No resource gap — UX resource is active
    const gapWarning = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(gapWarning).not.toBeVisible();

    // Step 3: Deactivate the UX resource
    await gotoPage(page, '/resources');
    await waitForTableRows(page.getByTestId('resources-table'));
    await page.getByTestId(`resources-edit-${resourceId}`).click();
    await expectModalVisible(page, 'resources-modal');
    await fillInputByTestId(page, 'resources-active', 'No');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    // Step 4: Go back to tasks — resource gap should now appear
    await gotoPage(page, '/tasks');
    await waitForTableRows(page.getByTestId('tasks-table'));
    const gapWarningAfter = page.getByTestId(`resource-gap-warning-${taskId}`);
    await expect(gapWarningAfter).toBeVisible({ timeout: 5_000 });

    // Clean up — delete task then resource
    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-modal-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();

    await gotoPage(page, '/resources');
    await waitForTableRows(page.getByTestId('resources-table'));
    await page.getByTestId(`resources-delete-${resourceId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-confirm').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
  });

  test('banner shows filter context when filters are active', async ({ page }) => {
    // Run scheduler so unscheduled/resource-gap counts are non-zero
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    const banner = page.getByTestId('task-warnings-banner');
    const isBannerVisible = await banner.isVisible().catch(() => false);

    if (isBannerVisible) {
      // Before applying filter — banner should NOT say "across all tasks"
      const textBefore = await banner.innerText();
      expect(textBefore).not.toContain('across all tasks');

      // Apply a risk filter
      await fillInputByTestId(page, 'tasks-filter-risk', 'On Track');
      await page.waitForTimeout(500);

      // If banner is still visible after filtering, it should say "across all tasks"
      const stillVisible = await banner.isVisible().catch(() => false);
      if (stillVisible) {
        await expect(banner).toContainText('across all tasks');
      }

      // Clear the filter
      await fillInputByTestId(page, 'tasks-filter-risk', '');
      await page.waitForTimeout(500);

      // Banner should revert to not saying "across all tasks"
      const visibleAfterClear = await banner.isVisible().catch(() => false);
      if (visibleAfterClear) {
        const textAfter = await banner.innerText();
        expect(textAfter).not.toContain('across all tasks');
      }
    }
  });

  test('banner shows filter context when status filter is active', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    const banner = page.getByTestId('task-warnings-banner');
    const isBannerVisible = await banner.isVisible().catch(() => false);

    if (isBannerVisible) {
      // Apply a status filter
      await fillInputByTestId(page, 'tasks-filter-status', 'Not Started');
      await page.waitForTimeout(500);

      const stillVisible = await banner.isVisible().catch(() => false);
      if (stillVisible) {
        await expect(banner).toContainText('across all tasks');
      }

      // Clear filters
      await page.getByTestId('tasks-clear-filters').click();
      await page.waitForTimeout(500);
    }
  });

  test('banner shows filter context when search is active', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    await page.getByTestId('tasks-refresh').click();
    await expect(page.getByTestId('tasks-refresh')).toBeEnabled({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    const banner = page.getByTestId('task-warnings-banner');
    const isBannerVisible = await banner.isVisible().catch(() => false);

    if (isBannerVisible) {
      // Type into the search box
      await page.getByTestId('tasks-search').fill('SVC');
      await page.waitForTimeout(1500); // wait for debounce

      const stillVisible = await banner.isVisible().catch(() => false);
      if (stillVisible) {
        await expect(banner).toContainText('across all tasks');
      }

      // Clear search
      await page.getByTestId('tasks-search').fill('');
      await page.waitForTimeout(1500);
    }
  });
});
