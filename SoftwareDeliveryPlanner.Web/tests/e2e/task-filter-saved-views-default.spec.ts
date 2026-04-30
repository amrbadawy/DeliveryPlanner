import { test, expect } from '@playwright/test';
import { gotoPage, uniqueSuffix } from './helpers';

test.describe('Saved views default behavior', () => {
  test.beforeEach(async ({ page }) => {
    await gotoPage(page, '/tasks');
    const clearAll = page.getByTestId('task-filter-clear-all');
    if (await clearAll.count()) {
      await clearAll.click();
    }
  });

  test.afterEach(async ({ page }) => {
    // Remove any saved views created by this spec so later specs keep their
    // historical assumptions (no default view auto-applied).
    await gotoPage(page, '/tasks');

    for (let i = 0; i < 25; i++) {
      const deleteButtons = page
        .getByTestId('task-filter-saved-views')
        .locator('[data-testid^="task-filter-saved-view-delete-"]');

      const count = await deleteButtons.count();
      if (count === 0) {
        break;
      }

      await deleteButtons.first().click({ force: true });
      await page.waitForTimeout(100);
    }
  });

  test('setting a default saved view auto-applies it on page reload', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-risk-late').click();
    const viewName = `Default ${uniqueSuffix('d')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(viewName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const savedList = page.getByTestId('task-filter-saved-views');
    const item = savedList.locator('li.task-filter-saved-item', { hasText: viewName }).first();
    await item.locator('[data-testid^="task-filter-saved-view-default-"]').click();

    await gotoPage(page, '/tasks');
    await expect(page.getByTestId('task-filter-chip-risk-late')).toHaveAttribute('aria-pressed', 'true');
  });

  test('only one saved view is default at a time', async ({ page }) => {
    await gotoPage(page, '/tasks');

    const riskLate = page.getByTestId('task-filter-chip-risk-late');
    if ((await riskLate.getAttribute('aria-pressed')) !== 'true') {
      await riskLate.click();
    }
    const firstName = `First ${uniqueSuffix('f')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(firstName);
    await page.getByTestId('task-filter-saved-view-save').click();

    await page.getByTestId('task-filter-clear-all').click();
    const priorityHigh = page.getByTestId('task-filter-chip-priority-high');
    if ((await priorityHigh.getAttribute('aria-pressed')) !== 'true') {
      await priorityHigh.click();
    }
    const secondName = `Second ${uniqueSuffix('s')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(secondName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const savedList = page.getByTestId('task-filter-saved-views');
    const firstItem = savedList.locator('li.task-filter-saved-item', { hasText: firstName }).first();
    const secondItem = savedList.locator('li.task-filter-saved-item', { hasText: secondName }).first();

    await firstItem.locator('[data-testid^="task-filter-saved-view-default-"]').click();
    await secondItem.locator('[data-testid^="task-filter-saved-view-default-"]').click();

    await expect(firstItem.locator('[data-testid^="task-filter-saved-view-default-"] i')).toHaveClass(/bi-star$/);
    await expect(secondItem.locator('[data-testid^="task-filter-saved-view-default-"] i')).toHaveClass(/bi-star-fill/);
  });

  test('default saved view does not cross page scope', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-status-not_started').click();
    const tasksViewName = `Tasks Default ${uniqueSuffix('t')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(tasksViewName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const savedList = page.getByTestId('task-filter-saved-views');
    const item = savedList.locator('li.task-filter-saved-item', { hasText: tasksViewName }).first();
    await item.locator('[data-testid^="task-filter-saved-view-default-"]').click();

    await gotoPage(page, '/gantt');
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('task-filter-saved-views')).not.toContainText(tasksViewName);
  });

  test('explicit ?view query overrides default auto-apply', async ({ page }) => {
    await gotoPage(page, '/tasks');

    await page.getByTestId('task-filter-chip-status-not_started').click();
    const defaultName = `Default URL ${uniqueSuffix('u1')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(defaultName);
    await page.getByTestId('task-filter-saved-view-save').click();

    await page.getByTestId('task-filter-clear-all').click();
    await page.getByTestId('task-filter-chip-priority-high').click();
    const overrideName = `Override URL ${uniqueSuffix('u2')}`;
    await page.getByTestId('task-filter-saved-view-name').fill(overrideName);
    await page.getByTestId('task-filter-saved-view-save').click();

    const savedList = page.getByTestId('task-filter-saved-views');
    const defaultItem = savedList.locator('li.task-filter-saved-item', { hasText: defaultName }).first();
    await defaultItem.locator('[data-testid^="task-filter-saved-view-default-"]').click();

    await gotoPage(page, `/tasks?view=${encodeURIComponent(overrideName)}`);
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'false');
  });
});
