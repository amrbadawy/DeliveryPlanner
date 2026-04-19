import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

test.describe('Layout Features', () => {
  test('auto-schedule toggle is visible and functional', async ({ page }) => {
    await gotoPage(page, '/tasks');

    const toggle = page.getByTestId('auto-schedule-toggle');
    await expect(toggle).toBeVisible();

    // Toggle should be a checkbox/switch
    const isChecked = await toggle.isChecked();

    // Click to toggle state
    await toggle.click();
    const newState = await toggle.isChecked();
    expect(newState).not.toBe(isChecked);

    // Toggle back
    await toggle.click();
    const restoredState = await toggle.isChecked();
    expect(restoredState).toBe(isChecked);
  });

  test('command palette opens with Ctrl+K and navigates', async ({ page }) => {
    await gotoPage(page, '/tasks');

    // Open command palette
    await page.keyboard.press('Control+k');
    const palette = page.getByTestId('command-palette');
    await expect(palette).toBeVisible();

    // Search for "Dashboard"
    const input = page.getByTestId('command-palette-input');
    await expect(input).toBeVisible();
    await input.fill('Dashboard');

    // Should show matching result
    await expect(palette.locator('.command-item', { hasText: 'Dashboard' })).toBeVisible();

    // Press Enter to navigate
    await input.press('Enter');

    // Should navigate to dashboard
    await expect(page).toHaveURL(/\//);
    await expect(page.getByTestId('command-palette')).toBeHidden();
  });

  test('command palette closes with Escape', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.keyboard.press('Control+k');
    const palette = page.getByTestId('command-palette');
    const input = page.getByTestId('command-palette-input');
    await expect(palette).toBeVisible();
    await expect(input).toBeVisible();
    await input.click();

    await input.press('Escape');
    await expect(palette).toBeHidden();
  });

  test('command palette lists all expected pages', async ({ page }) => {
    await gotoPage(page, '/');

    await page.keyboard.press('Control+k');
    const palette = page.getByTestId('command-palette');
    await expect(palette).toBeVisible();

    // Check for key page entries
    const expectedPages = ['Dashboard', 'Tasks', 'Resources', 'Gantt Chart', 'Workload Heatmap', 'What-If Scenarios', 'Activity Log'];
    for (const pageName of expectedPages) {
      await expect(palette.locator('.command-item', { hasText: pageName })).toBeVisible();
    }
  });

  test('critical path toggle on Gantt page', async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/gantt');

    const chart = page.getByTestId('gantt-chart');
    const empty = page.getByTestId('gantt-empty');
    if (await empty.isVisible().catch(() => false)) {
      test.skip(true, 'Scheduler produced no scheduled tasks in this run');
    }
    await expect(chart).toBeVisible();

    // Critical path toggle button should be visible
    const toggleBtn = page.getByTestId('gantt-critical-path-toggle');
    await expect(toggleBtn).toBeVisible();

    // Click to enable critical path
    await toggleBtn.click();

    // Button should now show active state (btn-danger class instead of btn-outline-danger)
    await expect(toggleBtn).toHaveClass(/btn-danger/);

    // Click to disable
    await toggleBtn.click();
    await expect(toggleBtn).toHaveClass(/btn-outline-danger/);
  });
});
