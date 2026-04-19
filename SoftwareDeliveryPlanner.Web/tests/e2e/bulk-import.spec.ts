import { test, expect } from '@playwright/test';
import { gotoPage, uniqueSuffix, waitForTableRows, fillInputByTestId, expectModalVisible } from './helpers';

test.describe('Bulk CSV Import', () => {
  test('open and close bulk import modal', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // Open bulk import modal
    const importBtn = page.locator('button', { hasText: 'Import CSV' });
    await expect(importBtn).toBeVisible();
    await importBtn.click();
    await expect(page.getByTestId('bulk-import-modal')).toBeVisible();

    // Close it
    const closeBtn = page.getByTestId('bulk-import-modal').locator('button', { hasText: 'Cancel' });
    await closeBtn.click();
    await expect(page.getByTestId('bulk-import-modal')).toBeHidden();
  });

  test.skip('import tasks from CSV text', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const initialCount = await table.locator('tbody tr').count();

    const suffix = Date.now().toString().slice(-4);
    const taskId = `SVC-E2E${suffix}`;
    const serviceName = uniqueSuffix('E2E Import');
    const csvLine = `${taskId},${serviceName},10,2,99,,`;

    // Open modal
    const importBtn = page.locator('button', { hasText: 'Import CSV' });
    await importBtn.click();
    await expect(page.getByTestId('bulk-import-modal')).toBeVisible();

    // Paste CSV — blur to ensure Blazor @bind completes via SignalR
    const csvTextarea = page.getByTestId('bulk-import-csv');
    await csvTextarea.fill(csvLine);
    await csvTextarea.blur();
    await page.waitForTimeout(1000);

    // Preview
    await page.getByTestId('bulk-import-preview').click();

    // Wait for preview to show parsed rows before clicking import
    await page.waitForTimeout(500);

    // Import
    await page.getByTestId('bulk-import-confirm').click();

    // Modal should close and new row should appear
    await expect(page.getByTestId('bulk-import-modal')).toBeHidden({ timeout: 10_000 });

    // Wait for table to refresh with new row
    await expect.poll(
      async () => await table.locator('tbody tr').count(),
      { timeout: 15_000 }
    ).toBeGreaterThan(initialCount);
  });
});
