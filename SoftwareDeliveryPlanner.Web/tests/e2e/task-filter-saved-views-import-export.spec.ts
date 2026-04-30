import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, uniqueSuffix } from './helpers';
import { promises as fs } from 'node:fs';
import * as path from 'node:path';
import * as os from 'node:os';

/**
 * E2E coverage for saved-views JSON export & import.
 */

test.describe('Saved views JSON export / import', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
  });

  test('export downloads a JSON file containing the saved views for the page', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-status-not_started').click();
    const viewName = `Export ${uniqueSuffix('e')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();
    await expect(page.getByTestId('task-filter-saved-views')).toContainText(viewName);

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('task-filter-saved-views-export').click(),
    ]);

    const suggested = download.suggestedFilename();
    expect(suggested).toMatch(/^saved-views-tasks-\d{8}-\d{6}\.json$/);

    const tmpPath = path.join(os.tmpdir(), suggested);
    await download.saveAs(tmpPath);

    const raw = await fs.readFile(tmpPath, 'utf-8');
    const dto = JSON.parse(raw);

    expect(dto.pageKey).toBe('tasks');
    expect(typeof dto.exportedAt).toBe('string');
    expect(Array.isArray(dto.views)).toBe(true);
    expect(dto.views.length).toBeGreaterThan(0);

    const exportedNames = dto.views.map((v: { name: string }) => v.name);
    expect(exportedNames).toContain(viewName);

    for (const v of dto.views) {
      expect(() => JSON.parse(v.payload)).not.toThrow();
    }

    await fs.unlink(tmpPath).catch(() => {});
  });

  test('import restores deleted views from a previously exported file', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-priority-high').click();
    const viewName = `Roundtrip ${uniqueSuffix('r')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();
    await expect(page.getByTestId('task-filter-saved-views')).toContainText(viewName);

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('task-filter-saved-views-export').click(),
    ]);
    const tmpPath = path.join(os.tmpdir(), download.suggestedFilename());
    await download.saveAs(tmpPath);

    const savedList = page.getByTestId('task-filter-saved-views');
    const deleteBtn = savedList.locator('[data-testid^="task-filter-saved-view-delete-"]').last();
    await deleteBtn.click();
    await expect(savedList).not.toContainText(viewName);

    const fileInput = page.getByTestId('task-filter-saved-views-import');
    await fileInput.setInputFiles(tmpPath);

    await expect(page.getByTestId('task-filter-saved-views-io-message')).toContainText(/Imported/);
    await expect(savedList).toContainText(viewName);

    await fs.unlink(tmpPath).catch(() => {});
  });

  test('importing the same file twice overwrites without creating duplicates', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-risk-late').click();
    const viewName = `Idempotent ${uniqueSuffix('i')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('task-filter-saved-views-export').click(),
    ]);
    const tmpPath = path.join(os.tmpdir(), download.suggestedFilename());
    await download.saveAs(tmpPath);

    const savedList = page.getByTestId('task-filter-saved-views');
    const fileInput = page.getByTestId('task-filter-saved-views-import');

    await fileInput.setInputFiles(tmpPath);
    await expect(page.getByTestId('task-filter-saved-views-io-message')).toContainText(/Imported/);

    await fileInput.setInputFiles(tmpPath);
    await expect(page.getByTestId('task-filter-saved-views-io-message')).toContainText(/Imported/);

    const matching = savedList.locator('[data-testid^="task-filter-saved-view-apply-"]', { hasText: viewName });
    await expect(matching).toHaveCount(1);

    await fs.unlink(tmpPath).catch(() => {});
  });

  test('importing a Tasks-page export on the Gantt page is rejected', async ({ page }) => {
    await gotoPage(page, '/tasks');
    await page.getByTestId('task-filter-chip-status-not_started').click();
    const viewName = `Cross-scope ${uniqueSuffix('x')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('task-filter-saved-views-export').click(),
    ]);
    const tmpPath = path.join(os.tmpdir(), download.suggestedFilename());
    await download.saveAs(tmpPath);

    await gotoPage(page, '/gantt');
    const fileInput = page.getByTestId('task-filter-saved-views-import');
    await fileInput.setInputFiles(tmpPath);

    await expect(page.getByTestId('task-filter-saved-views-io-error'))
      .toContainText(/exported for 'tasks'/i);
    await expect(page.getByTestId('task-filter-saved-views')).not.toContainText(viewName);

    await fs.unlink(tmpPath).catch(() => {});
  });

  test('importing a malformed file shows a JSON error and does not crash', async ({ page }) => {
    await gotoPage(page, '/tasks');

    const tmpPath = path.join(os.tmpdir(), `bad-${uniqueSuffix('b')}.json`);
    await fs.writeFile(tmpPath, '{ this is not valid json', 'utf-8');

    const fileInput = page.getByTestId('task-filter-saved-views-import');
    await fileInput.setInputFiles(tmpPath);

    await expect(page.getByTestId('task-filter-saved-views-io-error'))
      .toContainText(/Invalid JSON/i);

    await fs.unlink(tmpPath).catch(() => {});
  });
});
