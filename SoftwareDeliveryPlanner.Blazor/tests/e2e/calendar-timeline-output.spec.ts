import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, waitForTableRows } from './helpers';

test.describe('Calendar, timeline, output', () => {
  test('calendar refresh and rows visible', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/calendar');
    const table = page.getByTestId('calendar-table');
    await waitForTableRows(table);

    const before = await table.locator('tbody tr').count();
    await page.getByTestId('calendar-refresh').click();
    const after = await table.locator('tbody tr').count();
    expect(after).toBeGreaterThan(0);
    expect(after).toBe(before);
  });

  test('timeline refresh works and container renders day cards', async ({ page }) => {
    await gotoPage(page, '/timeline');
    const container = page.getByTestId('timeline-container');
    await expect(container).toBeVisible();

    await page.getByTestId('timeline-refresh').click();
    await expect(container.locator('div').first()).toBeVisible();

    await page.getByTestId('timeline-start-date').fill('2026-06-01');
    await page.getByTestId('timeline-end-date').fill('2026-06-10');
    await page.getByTestId('timeline-refresh').click();
    await expect(container).toBeVisible();
  });

  test('output refresh and csv export button works without crashing', async ({ page }) => {

    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/output');
    const table = page.getByTestId('output-table');
    await waitForTableRows(table);

    await page.getByTestId('output-refresh').click();
    await expect(table).toBeVisible();

    const beforeRows = await table.locator('tbody tr').count();
    await page.getByTestId('output-export-csv').click();

    await expect(table).toBeVisible();
    await expect(table.locator('tbody tr')).toHaveCount(beforeRows);
  });
});
