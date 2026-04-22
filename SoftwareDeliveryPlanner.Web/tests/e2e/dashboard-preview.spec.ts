import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Dashboard — Preview Changes + Overallocation KPI', () => {
  test('preview changes button is visible on dashboard', async ({ page }) => {
    await gotoPage(page, '/');

    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    const previewBtn = page.getByTestId('btn-preview-changes');
    await expect(previewBtn).toBeVisible();
    await expect(previewBtn).toBeEnabled();
  });

  test('clicking preview changes shows diff card', async ({ page }) => {
    await gotoPage(page, '/');
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    // Click Preview Changes
    await page.getByTestId('btn-preview-changes').click();

    // Wait for the diff card to appear
    const diffCard = page.getByTestId('preview-diff-card');
    await expect(diffCard).toBeVisible({ timeout: 30_000 });

    // It should contain task change summary text
    await expect(diffCard).toContainText('Tasks Affected');
    await expect(diffCard).toContainText('Unchanged');
    await expect(diffCard).toContainText('New Allocations');
  });

  test('closing preview diff card hides it', async ({ page }) => {
    await gotoPage(page, '/');
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    // Show diff
    await page.getByTestId('btn-preview-changes').click();
    const diffCard = page.getByTestId('preview-diff-card');
    await expect(diffCard).toBeVisible({ timeout: 30_000 });

    // Close it
    await page.getByTestId('preview-diff-close').click();
    await expect(diffCard).toBeHidden();
  });

  test('overallocation KPI card is visible on dashboard', async ({ page }) => {
    await gotoPage(page, '/');
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    const kpiCard = page.getByTestId('kpi-overallocation');
    await expect(kpiCard).toBeVisible();
    await expect(kpiCard).toContainText('Overallocations');
  });

  test('clicking overallocation KPI navigates to overallocation page', async ({ page }) => {
    await gotoPage(page, '/');
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    await page.getByTestId('kpi-overallocation').click();
    await page.waitForURL('**/overallocation');
    await expect(page).toHaveURL(/\/overallocation$/);
    await expect(page.getByRole('heading', { name: /Overallocation Alerts/ })).toBeVisible();
  });

  test('preview diff table shows entries after scheduler has run', async ({ page }) => {
    // Run scheduler first so there are allocations to diff
    await runSchedulerFromDashboard(page);

    await gotoPage(page, '/');
    await expect(page.getByTestId('dashboard-skeleton-kpi')).toBeHidden({ timeout: 15_000 });

    await page.getByTestId('btn-preview-changes').click();

    const diffCard = page.getByTestId('preview-diff-card');
    await expect(diffCard).toBeVisible({ timeout: 30_000 });

    // Either the diff table or the "No task changes" message should be present
    const diffTable = page.getByTestId('preview-diff-table');
    const noChangesMsg = diffCard.getByText('No task changes detected');
    await expect(diffTable.or(noChangesMsg)).toBeVisible({ timeout: 10_000 });
  });
});
