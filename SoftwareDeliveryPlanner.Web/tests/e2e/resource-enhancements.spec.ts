import { test, expect } from '@playwright/test';
import {
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';

test.describe('Resource enhancements — Seniority + Working Week', () => {
  test('add resource with seniority and working week', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    const resourceName = uniqueSuffix('E2E SenRes');

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    // Capture auto-generated ID
    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();

    await fillInputByTestId(page, 'resources-name', resourceName);
    await fillInputByTestId(page, 'resources-team', 'E2E Team');

    // Set seniority and working week
    const senioritySelect = page.getByTestId('resources-seniority');
    await expect(senioritySelect).toBeVisible();
    await senioritySelect.selectOption('Senior');

    const workingWeekSelect = page.getByTestId('resources-working-week');
    await expect(workingWeekSelect).toBeVisible();
    await workingWeekSelect.selectOption('MON_FRI');

    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    // Verify the resource appears in the table
    await expect(table.locator('tbody tr', { hasText: resourceName })).toHaveCount(1);

    // Clean up: delete the created resource
    await page.getByTestId(`resources-delete-${resourceId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-confirm').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
  });

  test('edit resource seniority and working week are populated', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    const resourceName = uniqueSuffix('E2E EditSen');

    // Create a resource with specific seniority
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();
    await fillInputByTestId(page, 'resources-name', resourceName);
    await page.getByTestId('resources-seniority').selectOption('Mid');
    await page.getByTestId('resources-working-week').selectOption('SUN_THU');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    // Edit the resource and verify fields are populated
    await page.getByTestId(`resources-edit-${resourceId}`).click();
    await expectModalVisible(page, 'resources-modal');

    const seniorityValue = await page.getByTestId('resources-seniority').inputValue();
    expect(seniorityValue).toBe('Mid');

    const workingWeekValue = await page.getByTestId('resources-working-week').inputValue();
    expect(workingWeekValue).toBe('SUN_THU');

    // Close modal and clean up
    await page.getByTestId('resources-cancel').click();
    await page.getByTestId(`resources-delete-${resourceId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-confirm').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
  });

  test('seniority defaults to Any and working week to Global Default', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    // Default values should be empty string (Any / Global Default)
    const seniorityValue = await page.getByTestId('resources-seniority').inputValue();
    expect(seniorityValue).toBe('');

    const workingWeekValue = await page.getByTestId('resources-working-week').inputValue();
    expect(workingWeekValue).toBe('');

    await page.getByTestId('resources-cancel').click();
  });
});
