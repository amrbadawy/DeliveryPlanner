import { test, expect } from '@playwright/test';
import { gotoPage } from './helpers';

test.describe('Settings page', () => {
  test('navigates to settings and shows strategy dropdown', async ({ page }) => {
    await gotoPage(page, '/');

    await page.getByTestId('nav-settings').click();
    await page.waitForURL('**/settings');
    await expect(page).toHaveURL(/\/settings$/);
    await expect(page.getByRole('heading', { name: /Settings/ })).toBeVisible();

    // Strategy dropdown should be visible after loading
    await expect(page.getByTestId('settings-skeleton')).toBeHidden({ timeout: 15_000 });
    const strategySelect = page.getByTestId('settings-strategy-select');
    await expect(strategySelect).toBeVisible();

    // Should have one of the valid strategy options selected
    const value = await strategySelect.inputValue();
    expect(['priority_first', 'deadline_first', 'balanced_workload', 'critical_path']).toContain(value);
  });

  test('save strategy shows success message', async ({ page }) => {
    await gotoPage(page, '/settings');
    await expect(page.getByTestId('settings-skeleton')).toBeHidden({ timeout: 15_000 });

    // Select a different strategy
    const strategySelect = page.getByTestId('settings-strategy-select');
    await strategySelect.selectOption('deadline_first');

    // Save
    await page.getByTestId('settings-strategy-save').click();

    // Expect success status message
    const status = page.getByTestId('settings-status');
    await expect(status).toBeVisible({ timeout: 10_000 });
    await expect(status).toContainText('saved');
  });

  test('freeze baseline shows success and date', async ({ page }) => {
    await gotoPage(page, '/settings');
    await expect(page.getByTestId('settings-skeleton')).toBeHidden({ timeout: 15_000 });

    // Click Freeze Baseline
    await page.getByTestId('settings-freeze-baseline').click();

    // Expect success status
    const status = page.getByTestId('settings-status');
    await expect(status).toBeVisible({ timeout: 10_000 });
    await expect(status).toContainText('frozen');

    // Baseline date should now show a date (not "Not set")
    const baselineDate = page.getByTestId('settings-baseline-date');
    await expect(baselineDate).toBeVisible();
    await expect(baselineDate).not.toHaveText('Not set');
  });

  test('clear baseline removes the date', async ({ page }) => {
    await gotoPage(page, '/settings');
    await expect(page.getByTestId('settings-skeleton')).toBeHidden({ timeout: 15_000 });

    // First freeze to ensure there's a date to clear
    await page.getByTestId('settings-freeze-baseline').click();
    await expect(page.getByTestId('settings-status')).toBeVisible({ timeout: 10_000 });

    // Clear button should now be visible
    const clearBtn = page.getByTestId('settings-clear-baseline');
    await expect(clearBtn).toBeVisible();
    await clearBtn.click();

    // Status should confirm clearing
    const status = page.getByTestId('settings-status');
    await expect(status).toContainText('cleared');

    // Baseline date should show "Not set"
    const baselineDate = page.getByTestId('settings-baseline-date');
    await expect(baselineDate).toHaveText('Not set');
  });
});
