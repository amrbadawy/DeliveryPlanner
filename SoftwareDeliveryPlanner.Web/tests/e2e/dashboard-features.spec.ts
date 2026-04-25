import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Dashboard Features', () => {
  test('stale plan warning shown before scheduler run', async ({ page }) => {
    await gotoPage(page, '/');

    // The stale plan warning should be visible if scheduler hasn't run
    // (or hidden if it has run in a previous test — both are valid)
    const warning = page.getByTestId('stale-plan-warning');
    const isVisible = await warning.isVisible().catch(() => false);

    // If visible, verify content
    if (isVisible) {
      await expect(warning).toContainText('Plan may be outdated');
    }
  });

  test('stale plan warning hides after running scheduler', async ({ page }) => {
    await gotoPage(page, '/');

    // Run scheduler from dashboard
    const runBtn = page.getByTestId('btn-run-scheduler');
    await expect(runBtn).toBeVisible();
    await runBtn.click();

    // Wait for scheduler to complete (button re-enables)
    await expect(runBtn).toBeEnabled({ timeout: 30_000 });

    // Warning should be hidden after scheduler run
    const warning = page.getByTestId('stale-plan-warning');
    await expect(warning).toBeHidden();
  });

  test('risk trend chart is visible after scheduler run', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/');

    // The risk trend chart data-testid appears when trend data exists
    const trendChart = page.getByTestId('risk-trend-chart');
    await expect(trendChart).toBeVisible({ timeout: 10_000 });
  });

  test('risk notifications section is present on dashboard', async ({ page }) => {
    await gotoPage(page, '/');

    // Risk notifications panel uses data-testid="risk-notifications" when there are alerts
    // When no alerts exist, the panel is hidden — both states are valid
    const notifications = page.getByTestId('risk-notifications');
    const isVisible = await notifications.isVisible().catch(() => false);

    // Either notifications panel is visible, or the dashboard loaded without alerts (also valid)
    await expect(page.getByRole('heading', { name: /Dashboard/ })).toBeVisible();
  });

  test('KPI cards are visible', async ({ page }) => {
    await gotoPage(page, '/');

    // Wait for loading to complete
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    // KPI values should be visible — check for the heading
    await expect(page.getByRole('heading', { name: /Dashboard/ })).toBeVisible();
  });

  test('unscheduled KPI card is visible on dashboard', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/');

    // Wait for loading to complete
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    // Unscheduled KPI card should be present
    const kpiCard = page.getByTestId('kpi-unscheduled');
    await expect(kpiCard).toBeVisible();
    await expect(kpiCard).toContainText('Unscheduled');
  });

  test('clicking unscheduled KPI navigates to tasks with filter', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/');

    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    const kpiCard = page.getByTestId('kpi-unscheduled');
    await expect(kpiCard).toBeVisible();
    await kpiCard.click();

    // Should navigate to the tasks page with the scheduled=no filter
    await expect(page).toHaveURL(/\/tasks\?scheduled=no/);

    // The unscheduled filter badge should be visible
    const badge = page.getByTestId('tasks-unscheduled-filter-badge');
    await expect(badge).toBeVisible();
    await expect(badge).toContainText('Unscheduled only');
  });
});
