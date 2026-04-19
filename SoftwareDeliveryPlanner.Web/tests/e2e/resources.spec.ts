import { test, expect } from '@playwright/test';
import {
  countRowsByText,
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';

test.describe('Resources CRUD + edge cases', () => {
  test('add, edit, refresh, and delete resource', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    const resourceName = uniqueSuffix('E2E Resource');
    const updatedName = `${resourceName} Updated`;

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();
    expect(resourceId).toMatch(/^DEV-\d{3,}$/);

    await fillInputByTestId(page, 'resources-name', resourceName);
    await fillInputByTestId(page, 'resources-team', 'E2E Team');
    await fillInputByTestId(page, 'resources-availability', '90');
    await fillInputByTestId(page, 'resources-daily-capacity', '0.9');
    await fillInputByTestId(page, 'resources-start-date', '2026-06-01');
    await fillInputByTestId(page, 'resources-notes', 'created by playwright');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: resourceName })).toHaveCount(1);
    await expect(page.getByTestId(`resources-row-${resourceId}`)).toContainText(resourceName);

    await page.getByTestId(`resources-edit-${resourceId}`).click();
    await expectModalVisible(page, 'resources-modal');
    await fillInputByTestId(page, 'resources-name', updatedName);
    await fillInputByTestId(page, 'resources-team', 'E2E Team Updated');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);
    await expect(page.getByTestId(`resources-row-${resourceId}`)).toContainText('E2E Team Updated');

    await page.getByTestId('resources-refresh').click();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);

    await page.getByTestId(`resources-delete-${resourceId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-confirm').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(0);
  });

  test('cancel add and cancel delete behaviors', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    const resourceName = uniqueSuffix('E2E Resource Cancel');

    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');
    await fillInputByTestId(page, 'resources-name', resourceName);
    await page.getByTestId('resources-cancel').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    expect(await countRowsByText(table, resourceName)).toBe(0);

    const firstRow = table.locator('tbody tr').first();
    const existingId = (await firstRow.locator('td').nth(0).innerText()).trim();
    const existingName = (await firstRow.locator('td').nth(1).innerText()).trim();

    await page.getByTestId(`resources-delete-${existingId}`).click();
    await expectModalVisible(page, 'resources-delete-modal');
    await page.getByTestId('resources-delete-modal-cancel').click();
    await expect(page.getByTestId('resources-delete-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: existingName })).toHaveCount(1);
    await expect(page.getByTestId(`resources-row-${existingId}`)).toBeVisible();
  });
});
