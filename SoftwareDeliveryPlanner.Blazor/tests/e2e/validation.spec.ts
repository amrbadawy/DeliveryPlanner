import { test, expect } from '@playwright/test';
import { expectModalVisible, fillInputByTestId, gotoPage } from './helpers';

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
});
