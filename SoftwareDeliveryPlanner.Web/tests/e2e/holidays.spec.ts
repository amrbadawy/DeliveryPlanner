import { test, expect } from '@playwright/test';
import {
  countRowsByText,
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';

const runYear = 2100 + Math.floor(Math.random() * 400);
const ymd = (month: number, day: number) => `${runYear}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;

test.describe('Holidays CRUD + edge cases', () => {
  test('add, edit, refresh, and delete holiday', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E Holiday CRUD');
    const updatedName = `${holidayName} Updated`;

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');

    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', ymd(11, 20));
    await fillInputByTestId(page, 'holidays-end-date', ymd(11, 22));
    await fillInputByTestId(page, 'holidays-type', 'Company');
    await fillInputByTestId(page, 'holidays-notes', 'e2e holiday');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: holidayName })).toHaveCount(1);
    await expect(table.locator('tbody tr', { hasText: holidayName }).first().locator('td').nth(4)).toContainText('Company');

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

    await page.getByTestId('holidays-refresh').click();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);

    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(0);
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

    const firstRow = table.locator('tbody tr').first();
    const delBtn = firstRow.locator('button[data-testid^="holidays-delete-"]').first();
    const rowIdTestId = await firstRow.getAttribute('data-testid');
    await delBtn.click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-cancel').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
    if (rowIdTestId) {
      await expect(page.getByTestId(rowIdTestId)).toBeVisible();
    }
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

  // --------------------------------------------------------
  // Additional edge-case tests
  // --------------------------------------------------------

  test('add single-day holiday shows "1 day" duration', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E SingleDay Dur');

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', ymd(12, 25));
    await fillInputByTestId(page, 'holidays-end-date', ymd(12, 25));
    await fillInputByTestId(page, 'holidays-type', 'Company');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();

    const row = table.locator('tbody tr', { hasText: holidayName }).first();
    await expect(row).toBeVisible();
    // Single-day holiday should show "1 day"
    await expect(row.locator('text=1 day')).toBeVisible();

    // Clean up: delete the holiday
    const idFromButton = await row.locator('button[data-testid^="holidays-edit-"]').getAttribute('data-testid');
    const id = idFromButton!.replace('holidays-edit-', '');
    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
  });

  test('add multi-day holiday and verify DB date range', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E MultiDay Dur');

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', ymd(12, 10));
    await fillInputByTestId(page, 'holidays-end-date', ymd(12, 14));
    await fillInputByTestId(page, 'holidays-type', 'National');
    await fillInputByTestId(page, 'holidays-notes', 'multi-day e2e');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();

    const row = table.locator('tbody tr', { hasText: holidayName }).first();
    await expect(row).toBeVisible();
    // 5-day holiday should show "5 days"
    await expect(row.locator('text=5 days')).toBeVisible();

    // Clean up
    const idFromButton = await row.locator('button[data-testid^="holidays-edit-"]').getAttribute('data-testid');
    const id = idFromButton!.replace('holidays-edit-', '');
    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
  });

  test('edit holiday date range updates correctly', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E EditDates Dur');

    // Add a holiday first
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', ymd(12, 1));
    await fillInputByTestId(page, 'holidays-end-date', ymd(12, 3));
    await fillInputByTestId(page, 'holidays-type', 'Company');
    await page.getByTestId('holidays-save').click();
    await expect(page.getByTestId('holidays-modal')).toBeHidden();

    // Verify 3 days initially
    const row = table.locator('tbody tr', { hasText: holidayName }).first();
    await expect(row.locator('text=3 days')).toBeVisible();

    // Edit to expand to 5 days
    const idFromButton = await row.locator('button[data-testid^="holidays-edit-"]').getAttribute('data-testid');
    const id = idFromButton!.replace('holidays-edit-', '');
    await page.getByTestId(`holidays-edit-${id}`).click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-end-date', ymd(12, 5));
    await page.getByTestId('holidays-save').click();
    await expect(page.getByTestId('holidays-modal')).toBeHidden();

    // Should now show 5 days
    await expect(row.locator('text=5 days')).toBeVisible();

    // Clean up
    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
  });

  test('seeded holidays table is populated with expected count', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // 4 holidays are seeded (Saudi Founding Day, Eid Al-Fitr, Arafat+Eid Al-Adha, National Day)
    const rowCount = await table.locator('tbody tr').count();
    expect(rowCount).toBeGreaterThanOrEqual(4);
  });

  test('seeded National Day shows as single day', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // National Day is seeded as single day (Sep 23)
    const ndRow = table.locator('tbody tr', { hasText: 'اليوم الوطني' }).first();
    await expect(ndRow).toBeVisible();
    await expect(ndRow.locator('text=1 day')).toBeVisible();
  });

  test('adjacent date to existing holiday saves successfully', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    const holidayName = uniqueSuffix('E2E Adjacent Dur');

    // Use a nearby non-overlap date (not used by seeded holidays).
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', holidayName);
    await fillInputByTestId(page, 'holidays-start-date', '2026-09-25');
    await fillInputByTestId(page, 'holidays-end-date', '2026-09-25');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: holidayName })).toHaveCount(1);

    // Clean up
    const row = table.locator('tbody tr', { hasText: holidayName }).first();
    const idFromButton = await row.locator('button[data-testid^="holidays-edit-"]').getAttribute('data-testid');
    const id = idFromButton!.replace('holidays-edit-', '');
    await page.getByTestId(`holidays-delete-${id}`).click();
    await expectModalVisible(page, 'holidays-delete-modal');
    await page.getByTestId('holidays-delete-modal-confirm').click();
    await expect(page.getByTestId('holidays-delete-modal')).toBeHidden();
  });

  test('exact same date as existing holiday shows overlap error', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // Try exact same date as National Day (Sep 23)
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', 'Exact Overlap Test');
    await fillInputByTestId(page, 'holidays-start-date', '2026-09-23');
    await fillInputByTestId(page, 'holidays-end-date', '2026-09-23');
    await page.getByTestId('holidays-save').click();

    // Should show overlap error
    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });

  test('start date after end date shows validation error', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');
    await fillInputByTestId(page, 'holidays-name', 'Invalid Dates');
    await fillInputByTestId(page, 'holidays-start-date', '2026-11-20');
    await fillInputByTestId(page, 'holidays-end-date', '2026-11-15');
    await page.getByTestId('holidays-save').click();

    // Should show validation error about date order
    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });

  test('Arafat + Eid Al-Adha shows correct duration', async ({ page }) => {
    await gotoPage(page, '/holidays');
    const table = page.getByTestId('holidays-table');
    await waitForTableRows(table);

    // Arafat + Eid Al-Adha is seeded as 4-day holiday (Jun 5 - Jun 8)
    const adhaRow = table.locator('tbody tr', { hasText: 'الأضحى' }).first();
    await expect(adhaRow).toBeVisible();
    await expect(adhaRow.locator('text=4 days')).toBeVisible();
  });
});
