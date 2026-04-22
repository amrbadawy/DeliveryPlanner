import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Analysis pages — Overallocation, Feasibility, Forecast', () => {
  // ── Overallocation ──────────────────────────────────────────

  test('overallocation page loads and shows empty state or table', async ({ page }) => {
    await gotoPage(page, '/');

    await page.getByTestId('nav-overallocation').click();
    await page.waitForURL('**/overallocation');
    await expect(page).toHaveURL(/\/overallocation$/);
    await expect(page.getByRole('heading', { name: /Overallocation Alerts/ })).toBeVisible();

    // After loading, either the empty state or the table should be visible
    const empty = page.getByTestId('overallocation-empty');
    const table = page.getByTestId('overallocation-table');

    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });

  test('overallocation refresh button works', async ({ page }) => {
    await gotoPage(page, '/overallocation');
    await expect(page.getByRole('heading', { name: /Overallocation Alerts/ })).toBeVisible();

    const refreshBtn = page.getByTestId('overallocation-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    // Wait for loading to complete — either empty or table appears
    const empty = page.getByTestId('overallocation-empty');
    const table = page.getByTestId('overallocation-table');
    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });

  // ── Feasibility ─────────────────────────────────────────────

  test('feasibility page loads and shows empty state or table', async ({ page }) => {
    await gotoPage(page, '/');

    await page.getByTestId('nav-feasibility').click();
    await page.waitForURL('**/feasibility');
    await expect(page).toHaveURL(/\/feasibility$/);
    await expect(page.getByRole('heading', { name: /Capacity Feasibility/ })).toBeVisible();

    // Seed data may or may not have strict-deadline tasks
    const empty = page.getByTestId('feasibility-empty');
    const table = page.getByTestId('feasibility-table');
    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });

  test('feasibility refresh button works', async ({ page }) => {
    await gotoPage(page, '/feasibility');
    await expect(page.getByRole('heading', { name: /Capacity Feasibility/ })).toBeVisible();

    const refreshBtn = page.getByTestId('feasibility-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    const empty = page.getByTestId('feasibility-empty');
    const table = page.getByTestId('feasibility-table');
    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });

  // ── Utilization Forecast ────────────────────────────────────

  test('forecast page loads and shows empty state or table', async ({ page }) => {
    await gotoPage(page, '/');

    await page.getByTestId('nav-forecast').click();
    await page.waitForURL('**/forecast');
    await expect(page).toHaveURL(/\/forecast$/);
    await expect(page.getByRole('heading', { name: /Utilization Forecast/ })).toBeVisible();

    const empty = page.getByTestId('forecast-empty');
    const table = page.getByTestId('forecast-table');
    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });

  test('forecast shows table with rows after scheduler run', async ({ page }) => {
    // Run the scheduler first to generate allocations
    await runSchedulerFromDashboard(page);

    await gotoPage(page, '/forecast');
    await expect(page.getByRole('heading', { name: /Utilization Forecast/ })).toBeVisible();

    // After a scheduler run, forecast should have week rows
    const table = page.getByTestId('forecast-table');
    await expect(table).toBeVisible({ timeout: 15_000 });

    // Should have at least 1 row in the tbody
    const rows = table.locator('tbody tr');
    await expect(rows.first()).toBeVisible();
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('forecast refresh button works', async ({ page }) => {
    await gotoPage(page, '/forecast');
    await expect(page.getByRole('heading', { name: /Utilization Forecast/ })).toBeVisible();

    const refreshBtn = page.getByTestId('forecast-refresh');
    await expect(refreshBtn).toBeVisible();
    await refreshBtn.click();

    const empty = page.getByTestId('forecast-empty');
    const table = page.getByTestId('forecast-table');
    await expect(empty.or(table)).toBeVisible({ timeout: 15_000 });
  });
});
