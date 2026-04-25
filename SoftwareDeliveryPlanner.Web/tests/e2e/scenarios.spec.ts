import { test, expect } from '@playwright/test';
import { gotoPage, uniqueSuffix, runSchedulerFromDashboard } from './helpers';

test.describe('What-If Scenarios', () => {
  test('scenarios page loads', async ({ page }) => {
    await gotoPage(page, '/scenarios');
    await expect(page.getByRole('heading', { name: /What-If Scenarios/ })).toBeVisible();
  });

  test('save, view, and delete a scenario', async ({ page }) => {
    // Run scheduler first so KPIs are populated
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/scenarios');

    const scenarioName = uniqueSuffix('E2E Scenario');

    // Open save modal
    await page.getByTestId('scenarios-save').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeVisible();

    // Fill and save
    await page.getByTestId('scenarios-name-input').fill(scenarioName);
    await page.getByTestId('scenarios-notes-input').fill('Automated test scenario');
    await page.getByTestId('scenarios-save-confirm').click();

    // Modal should close
    await expect(page.getByTestId('scenarios-save-modal')).toBeHidden();

    // Scenario should appear in table
    const table = page.getByTestId('scenarios-table');
    await expect(table).toBeVisible();
    await expect(table.locator('tbody tr', { hasText: scenarioName })).toHaveCount(1);

    // Delete the scenario
    const row = table.locator('tbody tr', { hasText: scenarioName });
    const deleteBtn = row.locator('button[data-testid^="scenarios-delete-"]');
    await deleteBtn.click();

    // Should be gone
    await expect(table.locator('tbody tr', { hasText: scenarioName })).toHaveCount(0);
  });

  test('cancel save modal does not create scenario', async ({ page }) => {
    await gotoPage(page, '/scenarios');
    const scenarioName = uniqueSuffix('E2E Cancel Scenario');

    await page.getByTestId('scenarios-save').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeVisible();
    await page.getByTestId('scenarios-name-input').fill(scenarioName);
    await page.getByTestId('scenarios-save-cancel').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeHidden();

    // Scenario should NOT appear
    const table = page.getByTestId('scenarios-table');
    if (await table.isVisible()) {
      await expect(table.locator('tbody tr', { hasText: scenarioName })).toHaveCount(0);
    }
  });

  test('compare two scenarios shows delta', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/scenarios');

    // Save two scenarios
    const nameA = uniqueSuffix('E2E Compare A');
    const nameB = uniqueSuffix('E2E Compare B');

    for (const name of [nameA, nameB]) {
      await page.getByTestId('scenarios-save').click();
      await expect(page.getByTestId('scenarios-save-modal')).toBeVisible();
      await page.getByTestId('scenarios-name-input').fill(name);
      await page.getByTestId('scenarios-save-confirm').click();
      await expect(page.getByTestId('scenarios-save-modal')).toBeHidden();
    }

    // Select both for comparison
    const selectA = page.getByTestId('scenarios-compare-a');
    const selectB = page.getByTestId('scenarios-compare-b');
    await expect(selectA).toBeVisible();
    await expect(selectB).toBeVisible();

    // Select first and second options (the scenarios we just created)
    const optionsA = selectA.locator('option');
    const countA = await optionsA.count();
    if (countA >= 2) {
      await selectA.selectOption({ index: countA - 2 });
      await selectB.selectOption({ index: countA - 1 });

      // Comparison section should become visible
      const comparison = page.getByTestId('scenarios-comparison');
      await expect(comparison).toBeVisible();

      // Comparison should include Unscheduled label
      await expect(comparison).toContainText('Unscheduled');
    }
  });

  test('scenario table shows Unscheduled column', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/scenarios');

    const scenarioName = uniqueSuffix('E2E Unsched Col');

    // Save a scenario
    await page.getByTestId('scenarios-save').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeVisible();
    await page.getByTestId('scenarios-name-input').fill(scenarioName);
    await page.getByTestId('scenarios-save-confirm').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeHidden();

    // Table should have Unscheduled header
    const table = page.getByTestId('scenarios-table');
    await expect(table).toBeVisible();
    await expect(table.locator('thead')).toContainText('Unscheduled');

    // Clean up
    const row = table.locator('tbody tr', { hasText: scenarioName });
    const deleteBtn = row.locator('button[data-testid^="scenarios-delete-"]');
    await deleteBtn.click();
    await expect(table.locator('tbody tr', { hasText: scenarioName })).toHaveCount(0);
  });
});
