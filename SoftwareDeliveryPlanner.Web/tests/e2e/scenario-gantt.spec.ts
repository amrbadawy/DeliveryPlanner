import { test, expect } from '@playwright/test';
import { gotoPage, uniqueSuffix, runSchedulerFromDashboard } from './helpers';

test.describe('Scenario Gantt Chart', () => {
  let scenarioName: string;

  test.beforeEach(async ({ page }) => {
    // Run the scheduler so tasks get scheduled dates, then save a scenario
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/scenarios');

    scenarioName = uniqueSuffix('E2E Gantt');

    await page.getByTestId('scenarios-save').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeVisible();
    await page.getByTestId('scenarios-name-input').fill(scenarioName);
    await page.getByTestId('scenarios-notes-input').fill('Gantt chart test scenario');
    await page.getByTestId('scenarios-save-confirm').click();
    await expect(page.getByTestId('scenarios-save-modal')).toBeHidden();

    // Wait for the scenario to appear in the table
    const table = page.getByTestId('scenarios-table');
    await expect(table.locator('tbody tr', { hasText: scenarioName })).toHaveCount(1);
  });

  test('view button navigates to scenario gantt page', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    const viewBtn = row.locator('button[data-testid^="scenarios-view-"]');
    await expect(viewBtn).toBeVisible();
    await viewBtn.click();

    // Should navigate to the scenario gantt page
    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);
    await expect(page.getByTestId('scenario-gantt-title')).toBeVisible();
    await expect(page.getByTestId('scenario-gantt-title')).toContainText(scenarioName);
  });

  test('scenario gantt page shows KPI summary', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);
    const kpis = page.getByTestId('scenario-gantt-kpis');
    await expect(kpis).toBeVisible();

    // KPIs should show the expected labels
    await expect(kpis).toContainText('Total Tasks');
    await expect(kpis).toContainText('On Track');
    await expect(kpis).toContainText('At Risk');
    await expect(kpis).toContainText('Late');
    await expect(kpis).toContainText('Unscheduled');
    await expect(kpis).toContainText('Latest Finish');
    await expect(kpis).toContainText('Total Est.');
  });

  test('scenario gantt chart renders with bars', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);

    const chart = page.getByTestId('scenario-gantt-chart');
    const empty = page.getByTestId('scenario-gantt-empty');

    // Either chart or empty state should be visible
    await expect.poll(async () => {
      const chartVisible = await chart.isVisible().catch(() => false);
      const emptyVisible = await empty.isVisible().catch(() => false);
      return chartVisible || emptyVisible;
    }, { timeout: 10_000 }).toBeTruthy();

    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks for this snapshot');
    }

    await expect(chart).toBeVisible();

    // Should have at least one row and bar
    const rows = chart.locator('[data-testid^="scenario-gantt-row-"]');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);

    const bars = chart.locator('[data-testid^="scenario-gantt-bar-"]');
    await expect(bars.first()).toBeVisible();
    expect(await bars.count()).toBeGreaterThan(0);
  });

  test('scenario gantt legend is visible', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);

    const empty = page.getByTestId('scenario-gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks for this snapshot');
    }

    const legend = page.getByTestId('scenario-gantt-legend');
    await expect(legend).toBeVisible();
    await expect(legend).toContainText('Completed');
    await expect(legend).toContainText('In Progress');
    await expect(legend).toContainText('Not Started');
    await expect(legend).toContainText('At Risk');
    await expect(legend).toContainText('Late');
  });

  test('scenario gantt shows plan date range', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);

    const empty = page.getByTestId('scenario-gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks for this snapshot');
    }

    const range = page.getByTestId('scenario-gantt-range');
    await expect(range).toBeVisible();
    await expect(range).toContainText('Plan range:');
    await expect(range).toContainText('days');
  });

  test('critical path toggle works', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);

    const toggle = page.getByTestId('scenario-gantt-critical-path-toggle');
    await expect(toggle).toBeVisible();

    // Initially should be outline (not active)
    await expect(toggle).toHaveClass(/btn-outline-danger/);

    // Click to enable
    await toggle.click();
    await expect(toggle).toHaveClass(/btn-danger/);
    await expect(toggle).not.toHaveClass(/btn-outline-danger/);

    // Legend should now include "Critical Path"
    const legend = page.getByTestId('scenario-gantt-legend');
    if (await legend.isVisible().catch(() => false)) {
      await expect(legend).toContainText('Critical Path');
    }

    // Click again to disable
    await toggle.click();
    await expect(toggle).toHaveClass(/btn-outline-danger/);
  });

  test('back button navigates to scenarios page', async ({ page }) => {
    const table = page.getByTestId('scenarios-table');
    const row = table.locator('tbody tr', { hasText: scenarioName });
    await row.locator('button[data-testid^="scenarios-view-"]').click();

    await expect(page).toHaveURL(/\/scenarios\/\d+\/gantt/);

    const backBtn = page.getByTestId('scenario-gantt-back');
    await expect(backBtn).toBeVisible();
    await backBtn.click();

    // Should navigate back to /scenarios
    await expect(page).toHaveURL(/\/scenarios$/);
    await expect(page.getByRole('heading', { name: /What-If Scenarios/ })).toBeVisible();
  });

  test('non-existent scenario shows not-found state', async ({ page }) => {
    await gotoPage(page, '/scenarios/999999/gantt');

    const notFound = page.getByTestId('scenario-gantt-not-found');
    await expect(notFound).toBeVisible();
    await expect(notFound).toContainText('Scenario not found');
  });
});
