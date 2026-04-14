import { test, expect } from '@playwright/test';
import { gotoPage } from './helpers';

test.describe('Smoke suite', () => {
  test('app loads dashboard and scheduler button is available', async ({ page }) => {
    await gotoPage(page, '/');
    await expect(page.getByRole('heading', { name: /Dashboard/ })).toBeVisible();
    await expect(page.getByTestId('btn-run-scheduler')).toBeVisible();
  });

  test('critical navigation works', async ({ page }) => {
    await gotoPage(page, '/');

    await page.getByTestId('nav-tasks').click();
    await page.waitForURL('**/tasks');
    await expect(page).toHaveURL(/\/tasks$/);
    await expect(page.getByTestId('tasks-table')).toBeVisible();

    await page.getByTestId('nav-resources').click();
    await page.waitForURL('**/resources');
    await expect(page).toHaveURL(/\/resources$/);
    await expect(page.getByTestId('resources-table')).toBeVisible();

    await page.getByTestId('nav-output').click();
    await page.waitForURL('**/output');
    await expect(page).toHaveURL(/\/output$/);
    await expect(page.getByTestId('output-table')).toBeVisible();
  });

  test('core CRUD entry points open modals', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('tasks-add').click();
    await expect(page.getByTestId('tasks-modal')).toBeVisible();
    await page.getByTestId('tasks-cancel').click();

    await gotoPage(page, '/resources');
    await page.getByTestId('resources-add').click();
    await expect(page.getByTestId('resources-modal')).toBeVisible();
    await page.getByTestId('resources-cancel').click();

    await gotoPage(page, '/holidays');
    await page.getByTestId('holidays-add').click();
    await expect(page.getByTestId('holidays-modal')).toBeVisible();
    await page.getByTestId('holidays-cancel').click();
  });
});
