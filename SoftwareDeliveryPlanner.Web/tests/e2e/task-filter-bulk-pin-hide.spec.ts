import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, waitForTableRows } from './helpers';

test.describe('Task bulk pin/hide', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('select all visible and hide selected removes all visible rows', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table, 1);

    await page.getByTestId('tasks-select-all-visible').check();
    await expect(page.getByTestId('tasks-bulk-toolbar')).toBeVisible();
    await page.getByTestId('tasks-bulk-hide').click();

    await expect(page.getByTestId('tasks-no-results')).toBeVisible();
  });
});
