import { test, expect } from '@playwright/test';
import {
  countRowsByText,
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';
import { countHolidaysByName, getHolidayByName } from './db-assertions';

test.describe('Holidays CRUD + edge cases', () => {
  test('add, edit, refresh, and delete holiday', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E Holiday');
    const updatedName = `${holidayName} Updated`;

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');

    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', '2026-11-20');
    await fillInputByTestId(page, 'holidays-end-date', '2026-11-22');
    await fillInputByTestId(page, 'holidays-type', 'Company');
    await fillInputByTestId(page, 'holidays-notes', 'e2e holiday');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: holidayName })).toHaveCount(1);
    expect(countHolidaysByName(holidayName)).toBe(1);
    expect(getHolidayByName(holidayName)?.holidayType).toBe('Company');

    const row = table.locator('tbody tr', { hasText: holidayName }).first();
    const idFromButton = await row.locator('button[data-testid^="holidays-edit-"]').getAttribute('data-testid');
    expect(idFromButton).toBeTruthy();
    const id = idFromButton!.replace('holidays-edit-', '');

    await page.getByTestId(`holidays-edit-${id}`).click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', updatedName);
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);
    expect(countHolidaysByName(holidayName)).toBe(0);
    expect(countHolidaysByName(updatedName)).toBe(1);

    await page.getByTestId('holidays-refresh').click();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);

    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(0);
    expect(countHolidaysByName(updatedName)).toBe(0);
  });

  test('cancel add and cancel delete', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E Holiday Cancel');
    const before = await countRowsByText(table, holidayName);

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', holidayName);
    await page.getByTestId('holidays-cancel').click();
    await expect(page.getByTestId('holidays-modal')).toBeHidden();

    expect(await countRowsByText(table, holidayName)).toBe(before);
    expect(countHolidaysByName(holidayName)).toBe(before);

    const firstRow = table.locator('tbody tr').first();
    const delBtn = firstRow.locator('button[data-testid^="holidays-delete-"]').first();
    await delBtn.click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-cancel').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
    await expect(firstRow).toBeVisible();
  });

  test('edge: empty holiday name does not persist row', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const before = await table.locator('tbody tr').count();
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', '');
    await page.getByTestId('holidays-save').click();

    // Validation should prevent saving - modal should still be open or error shown
    await page.getByTestId('holidays-cancel').click();
    const after = await table.locator('tbody tr').count();
    expect(after).toBe(before);
  });

  test('holiday shows duration for multi-day range', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // Eid Al-Fitr is seeded as a 4-day holiday (Mar 30 - Apr 2)
    const eidRow = table.locator('tbody tr', { hasText: 'الفطر' }).first();
    await expect(eidRow).toBeVisible();
    // Should show "4 days" duration
    await expect(eidRow.locator('text=4 days')).toBeVisible();
  });

  test('overlap validation prevents saving', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // Try to add a holiday that overlaps with National Day (Sep 23)
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', 'Overlap Test');
    await fillInputByTestId(page, 'holidays-start-date', '2026-09-22');
    await fillInputByTestId(page, 'holidays-end-date', '2026-09-24');
    await page.getByTestId('holidays-save').click();

    // Should show error about overlap
    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });
});
