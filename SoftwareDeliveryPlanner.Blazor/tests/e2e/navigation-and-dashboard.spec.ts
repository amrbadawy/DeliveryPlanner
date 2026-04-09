import { test, expect } from '@playwright/test';
import { gotoPage } from './helpers';

test.describe('Navigation and dashboard', () => {
  test('navigates to all main pages', async ({ page }) => {
    await gotoPage(page, '/');

    await expect(page.getByText('Dashboard')).toBeVisible();

    await page.getByTestId('nav-tasks').click();
    await expect(page).toHaveURL(/\/tasks$/);
    await expect(page.getByText('Task Register')).toBeVisible();

    await page.getByTestId('nav-resources').click();
    await expect(page).toHaveURL(/\/resources$/);
    await expect(page.getByText('Resources')).toBeVisible();

    await page.getByTestId('nav-adjustments').click();
    await expect(page).toHaveURL(/\/adjustments$/);
    await expect(page.getByText('Adjustments')).toBeVisible();

    await page.getByTestId('nav-holidays').click();
    await expect(page).toHaveURL(/\/holidays$/);
    await expect(page.getByText('Holidays')).toBeVisible();

    await page.getByTestId('nav-calendar').click();
    await expect(page).toHaveURL(/\/calendar$/);
    await expect(page.getByText('Calendar')).toBeVisible();

    await page.getByTestId('nav-timeline').click();
    await expect(page).toHaveURL(/\/timeline$/);
    await expect(page.getByText('Employee Timeline')).toBeVisible();

    await page.getByTestId('nav-output').click();
    await expect(page).toHaveURL(/\/output$/);
    await expect(page.getByText('Delivery Plan')).toBeVisible();
  });

  test('runs scheduler from dashboard and shows message', async ({ page }) => {
    await gotoPage(page, '/');

    const runBtn = page.getByTestId('btn-run-scheduler');
    await expect(runBtn).toBeVisible();
    await runBtn.click();

    await expect(page.getByTestId('scheduler-result')).toContainText('Successfully scheduled');
  });
});
