import { test, expect } from '@playwright/test';
import { expectModalVisible, fillInputByTestId, gotoPage, waitForTableRows } from './helpers';

test.describe('Validation and exceptional flows', () => {
  test('tasks validation blocks invalid values', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', '');
    await fillInputByTestId(page, 'tasks-dev-estimation', '0');
    await fillInputByTestId(page, 'tasks-max-dev', '0');
    await fillInputByTestId(page, 'tasks-priority', '11');
    await page.getByTestId('tasks-save').click();

    await expect(page.getByTestId('tasks-error')).toBeVisible();
    await expect(page.getByTestId('tasks-modal')).toBeVisible();
    await page.getByTestId('tasks-cancel').click();
  });

  test('resources validation blocks invalid values', async ({ page }) => {
    await gotoPage(page, '/resources');
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    await fillInputByTestId(page, 'resources-name', '');
    await fillInputByTestId(page, 'resources-availability', '140');
    await fillInputByTestId(page, 'resources-daily-capacity', '0');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-error')).toBeVisible();
    await expect(page.getByTestId('resources-modal')).toBeVisible();
    await page.getByTestId('resources-cancel').click();
  });

  test('adjustments validation blocks invalid values', async ({ page }) => {
    await gotoPage(page, '/adjustments');
    await page.getByTestId('adjustments-add').click();
    await expectModalVisible(page, 'adjustments-modal');

    await fillInputByTestId(page, 'adjustments-availability', '200');
    await fillInputByTestId(page, 'adjustments-start-date', '2026-09-10');
    await fillInputByTestId(page, 'adjustments-end-date', '2026-09-01');
    await page.getByTestId('adjustments-save').click();

    await expect(page.getByTestId('adjustments-error')).toBeVisible();
    await expect(page.getByTestId('adjustments-modal')).toBeVisible();
    await page.getByTestId('adjustments-cancel').click();
  });

  test('holidays validation blocks empty holiday name', async ({ page }) => {
    await gotoPage(page, '/holidays');
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');

    await fillInputByTestId(page, 'holidays-name', '');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await expect(page.getByTestId('holidays-modal')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });

  // --------------------------------------------------------
  // Additional validation edge-case tests
  // --------------------------------------------------------

  test('tasks validation blocks negative estimation', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', 'Negative Test');
    await fillInputByTestId(page, 'tasks-dev-estimation', '-5');
    await fillInputByTestId(page, 'tasks-max-dev', '1');
    await fillInputByTestId(page, 'tasks-priority', '5');
    await page.getByTestId('tasks-save').click();

    await expect(page.getByTestId('tasks-error')).toBeVisible();
    await expect(page.getByTestId('tasks-modal')).toBeVisible();
    await page.getByTestId('tasks-cancel').click();
  });

  test('tasks validation blocks priority zero', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', 'Priority Zero');
    await fillInputByTestId(page, 'tasks-dev-estimation', '5');
    await fillInputByTestId(page, 'tasks-max-dev', '1');
    await fillInputByTestId(page, 'tasks-priority', '0');
    await page.getByTestId('tasks-save').click();

    await expect(page.getByTestId('tasks-error')).toBeVisible();
    await expect(page.getByTestId('tasks-modal')).toBeVisible();
    await page.getByTestId('tasks-cancel').click();
  });

  test('resources validation blocks negative availability', async ({ page }) => {
    await gotoPage(page, '/resources');
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    await fillInputByTestId(page, 'resources-name', 'Negative Avail');
    await fillInputByTestId(page, 'resources-availability', '-10');
    await fillInputByTestId(page, 'resources-daily-capacity', '1');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-error')).toBeVisible();
    await expect(page.getByTestId('resources-modal')).toBeVisible();
    await page.getByTestId('resources-cancel').click();
  });

  test('resources validation blocks negative capacity', async ({ page }) => {
    await gotoPage(page, '/resources');
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    await fillInputByTestId(page, 'resources-name', 'Negative Cap');
    await fillInputByTestId(page, 'resources-availability', '80');
    await fillInputByTestId(page, 'resources-daily-capacity', '-1');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-error')).toBeVisible();
    await expect(page.getByTestId('resources-modal')).toBeVisible();
    await page.getByTestId('resources-cancel').click();
  });

  test('holidays validation blocks start date after end date', async ({ page }) => {
    await gotoPage(page, '/holidays');
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');

    await fillInputByTestId(page, 'holidays-name', 'Date Order Test');
    await fillInputByTestId(page, 'holidays-start-date', '2026-12-20');
    await fillInputByTestId(page, 'holidays-end-date', '2026-12-10');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await expect(page.getByTestId('holidays-modal')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });

  test('holidays overlap validation blocks enveloping range', async ({ page }) => {
    await gotoPage(page, '/holidays');
    await page.getByTestId('holidays-add').click();
    await expectModalVisible(page, 'holidays-modal');

    // Eid Al-Fitr is Mar 30 - Apr 2. Try to envelope it with Mar 28 - Apr 5.
    await fillInputByTestId(page, 'holidays-name', 'Envelope Test');
    await fillInputByTestId(page, 'holidays-start-date', '2026-03-28');
    await fillInputByTestId(page, 'holidays-end-date', '2026-04-05');
    await page.getByTestId('holidays-save').click();

    await expect(page.getByTestId('holidays-error')).toBeVisible();
    await expect(page.getByTestId('holidays-modal')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });

  test('valid task with all fields filled saves and closes modal', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const before = await table.locator('tbody tr').count();
    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', 'Valid Task E2E');
    await fillInputByTestId(page, 'tasks-dev-estimation', '5');
    await fillInputByTestId(page, 'tasks-max-dev', '2');
    await fillInputByTestId(page, 'tasks-priority', '5');
    await page.getByTestId('tasks-save').click();

    await expect(page.getByTestId('tasks-modal')).toBeHidden();
    const after = await table.locator('tbody tr').count();
    expect(after).toBeGreaterThan(before);
  });

  test('valid resource with all fields filled saves and closes modal', async ({ page }) => {
    await gotoPage(page, '/resources');
    const table = page.getByTestId('resources-table');
    await waitForTableRows(table);

    const before = await table.locator('tbody tr').count();
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');

    await fillInputByTestId(page, 'resources-name', 'Valid Resource E2E');
    await fillInputByTestId(page, 'resources-availability', '80');
    await fillInputByTestId(page, 'resources-daily-capacity', '1');
    await fillInputByTestId(page, 'resources-start-date', '2026-06-01');
    await page.getByTestId('resources-save').click();

    await expect(page.getByTestId('resources-modal')).toBeHidden();
    const after = await table.locator('tbody tr').count();
    expect(after).toBeGreaterThan(before);
  });
});
