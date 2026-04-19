import { test, expect } from '@playwright/test';
import { gotoPage } from './helpers';

test.describe('Navigation and dashboard', () => {
  test('navigates to all main pages', async ({ page }) => {
    await gotoPage(page, '/');

    await expect(page.getByRole('heading', { name: /Dashboard/ })).toBeVisible();

    await page.getByTestId('nav-tasks').click();
    await expect(page).toHaveURL(/\/tasks$/);
    await expect(page.getByRole('heading', { name: /Task Register/ })).toBeVisible();

    await page.getByTestId('nav-roles').click();
    await expect(page).toHaveURL(/\/roles$/);
    await expect(page.getByRole('heading', { name: /Roles/ })).toBeVisible();

    await page.getByTestId('nav-resources').click();
    await expect(page).toHaveURL(/\/resources$/);
    await expect(page.getByRole('heading', { name: /Resources/ })).toBeVisible();

    await page.getByTestId('nav-adjustments').click();
    await expect(page).toHaveURL(/\/adjustments$/);
    await expect(page.getByRole('heading', { name: /Adjustments/ })).toBeVisible();

    await page.getByTestId('nav-holidays').click();
    await expect(page).toHaveURL(/\/holidays$/);
    await expect(page.getByRole('heading', { name: /Holidays/ })).toBeVisible();

    await page.getByTestId('nav-calendar').click();
    await expect(page).toHaveURL(/\/calendar$/);
    await expect(page.getByRole('heading', { name: /Calendar/ })).toBeVisible();

    await page.getByTestId('nav-timeline').click();
    await expect(page).toHaveURL(/\/timeline$/);
    await expect(page.getByRole('heading', { name: /Employee Timeline/ })).toBeVisible();

    await page.getByTestId('nav-output').click();
    await expect(page).toHaveURL(/\/output$/);
    await expect(page.getByRole('heading', { name: /Delivery Plan/ })).toBeVisible();

    await page.getByTestId('nav-gantt').click();
    await expect(page).toHaveURL(/\/gantt$/);
    await expect(page.getByRole('heading', { name: /Gantt Chart/ })).toBeVisible();
  });

  test('runs scheduler from dashboard and shows message', async ({ page }) => {
    await gotoPage(page, '/');

    const runBtn = page.getByTestId('btn-run-scheduler');
    await expect(runBtn).toBeVisible();
    await runBtn.click();

    await expect(page.getByTestId('scheduler-result')).toContainText('Successfully scheduled');
  });
});
