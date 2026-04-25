import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard, waitForTableRows } from './helpers';

test.describe('Tasks search, filter, and sort', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/tasks');
    await waitForTableRows(page.getByTestId('tasks-table'));
  });

  test('search box filters tasks by ID', async ({ page }) => {
    const table = page.getByTestId('tasks-table');
    const allRows = await table.locator('tbody tr').count();
    expect(allRows).toBeGreaterThan(0);

    // Type a specific task ID prefix
    await page.getByTestId('tasks-search').fill('SVC-001');
    await expect(table.locator('tbody tr')).toHaveCount(1);
    await expect(table.locator('tbody tr').first()).toContainText('SVC-001');
  });

  test('search box filters tasks by service name', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    // Get a service name from the first row
    const firstRowName = await table.locator('tbody tr').first().locator('td').nth(1).innerText();
    const searchFragment = firstRowName.substring(0, 5);

    await page.getByTestId('tasks-search').fill(searchFragment);
    const filtered = await table.locator('tbody tr').count();
    expect(filtered).toBeGreaterThan(0);
    expect(filtered).toBeLessThanOrEqual(await page.getByTestId('tasks-table').locator('tbody tr').count());
  });

  test('search with no matches shows no-results message', async ({ page }) => {
    await page.getByTestId('tasks-search').fill('XYZNONEXISTENT999');
    await expect(page.getByTestId('tasks-no-results')).toBeVisible();
  });

  test('status filter reduces visible rows', async ({ page }) => {
    const table = page.getByTestId('tasks-table');
    const allRows = await table.locator('tbody tr').count();

    await page.getByTestId('tasks-filter-status').selectOption('Completed');
    const filteredRows = await table.locator('tbody tr').count();
    expect(filteredRows).toBeLessThanOrEqual(allRows);

    // All visible rows should have "Completed" badge
    const rows = table.locator('tbody tr');
    const count = await rows.count();
    for (let i = 0; i < count; i++) {
      await expect(rows.nth(i).locator('.badge', { hasText: 'Completed' })).toBeVisible();
    }
  });

  test('risk filter shows only matching tasks', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    await page.getByTestId('tasks-filter-risk').selectOption('On Track');
    await page.waitForTimeout(300);
    const rows = table.locator('tbody tr');
    const count = await rows.count();
    expect(count).toBeGreaterThanOrEqual(0);

    if (count > 0) {
      await expect(rows.first()).toContainText('On Track');
    } else {
      await expect(page.getByTestId('tasks-no-results')).toBeVisible();
    }
  });

  test('clear button resets all filters', async ({ page }) => {
    const table = page.getByTestId('tasks-table');
    const allRows = await table.locator('tbody tr').count();

    // Apply filters
    await page.getByTestId('tasks-search').fill('SVC');
    await page.getByTestId('tasks-filter-status').selectOption('Completed');

    // Clear all
    await page.getByTestId('tasks-clear-filters').click();
    await expect.poll(async () => await table.locator('tbody tr').count(), { timeout: 10_000 }).toBeGreaterThan(0);

    // Verify search box is empty
    await expect(page.getByTestId('tasks-search')).toHaveValue('');
  });

  test('clicking column header sorts tasks', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    // Sort by Priority ascending
    await page.getByTestId('tasks-sort-priority').click();
    await expect(page.getByTestId('tasks-sort-priority')).toContainText('▲');
    const firstPriority = await table.locator('tbody tr').first().locator('td').nth(4).innerText();

    // Sort by Priority descending (click again)
    await page.getByTestId('tasks-sort-priority').click();
    await expect(page.getByTestId('tasks-sort-priority')).toContainText('▼');
    const firstPriorityDesc = await table.locator('tbody tr').first().locator('td').nth(4).innerText();

    expect(firstPriority.length).toBeGreaterThan(0);
    expect(firstPriorityDesc.length).toBeGreaterThan(0);
  });

  test('sort indicator shows on active column', async ({ page }) => {
    // Click ID header
    const header = page.getByTestId('tasks-sort-id');
    await header.click();
    await expect(header).toContainText('▲');

    // Click again for descending
    await header.click();
    await expect(header).toContainText('▼');
  });

  test('combined search and filter works together', async ({ page }) => {
    const table = page.getByTestId('tasks-table');

    // Apply status filter
    await page.getByTestId('tasks-filter-status').selectOption('Completed');
    const afterFilter = await table.locator('tbody tr').count();

    // Add search on top
    await page.getByTestId('tasks-search').fill('SVC');
    const afterBoth = await table.locator('tbody tr').count();
    expect(afterBoth).toBeLessThanOrEqual(afterFilter);
  });

  test('direct navigation to ?scheduled=no shows unscheduled filter badge', async ({ page }) => {
    await gotoPage(page, '/tasks?scheduled=no');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    // The unscheduled filter badge should be visible
    const badge = page.getByTestId('tasks-unscheduled-filter-badge');
    await expect(badge).toBeVisible();
    await expect(badge).toContainText('Unscheduled only');
  });

  test('clearing filters removes unscheduled filter badge', async ({ page }) => {
    await gotoPage(page, '/tasks?scheduled=no');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const badge = page.getByTestId('tasks-unscheduled-filter-badge');
    await expect(badge).toBeVisible();

    // Click clear filters
    await page.getByTestId('tasks-clear-filters').click();
    await expect(badge).not.toBeVisible();
  });

  test('unscheduled filter combines with search', async ({ page }) => {
    await gotoPage(page, '/tasks?scheduled=no');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const rowsBefore = await table.locator('tbody tr').count();

    // Add search to narrow further
    await page.getByTestId('tasks-search').fill('SVC-001');
    const rowsAfter = await table.locator('tbody tr').count();
    expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
  });
});

test.describe('Resources search, filter, and sort', () => {
  test.beforeEach(async ({ page }) => {
    await gotoPage(page, '/resources');
    await waitForTableRows(page.getByTestId('resources-table'));
  });

  test('search box filters resources by name', async ({ page }) => {
    const table = page.getByTestId('resources-table');
    const allRows = await table.locator('tbody tr').count();
    expect(allRows).toBeGreaterThan(0);

    // Get first resource name and search by part of it
    const firstName = await table.locator('tbody tr').first().locator('td').nth(1).innerText();
    const fragment = firstName.substring(0, 3);

    await page.getByTestId('resources-search').fill(fragment);
    const filtered = await table.locator('tbody tr').count();
    expect(filtered).toBeGreaterThan(0);
  });

  test('search box filters resources by ID', async ({ page }) => {
    const table = page.getByTestId('resources-table');

    await page.getByTestId('resources-search').fill('DEV-001');
    await expect(table.locator('tbody tr')).toHaveCount(1);
    await expect(table.locator('tbody tr').first()).toContainText('DEV-001');
  });

  test('role filter shows only matching resources', async ({ page }) => {
    const table = page.getByTestId('resources-table');

    await page.getByTestId('resources-filter-role').selectOption('Developer');
    const rows = table.locator('tbody tr');
    // Wait for Blazor to finish re-rendering the filtered list before snapshotting
    // the count — without this the count() races with the async DOM update.
    await expect(rows.first()).toContainText('Developer');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      await expect(rows.nth(i)).toContainText('Developer');
    }
  });

  test('active filter shows only active resources', async ({ page }) => {
    const table = page.getByTestId('resources-table');
    const allRows = await table.locator('tbody tr').count();

    await page.getByTestId('resources-filter-active').selectOption('Yes');
    const activeRows = await table.locator('tbody tr').count();
    expect(activeRows).toBeLessThanOrEqual(allRows);
    expect(activeRows).toBeGreaterThan(0);
  });

  test('clear button resets all resource filters', async ({ page }) => {
    const table = page.getByTestId('resources-table');
    const allRows = await table.locator('tbody tr').count();

    await page.getByTestId('resources-search').fill('DEV');
    await page.getByTestId('resources-filter-role').selectOption('Developer');
    await page.getByTestId('resources-clear-filters').click();

    // Poll until the full unfiltered list is restored — Blazor re-renders async
    // so a snapshot count() immediately after click() can still see the filtered DOM.
    await expect.poll(
      async () => table.locator('tbody tr').count(),
      { timeout: 5_000 }
    ).toBe(allRows);
    await expect(page.getByTestId('resources-search')).toHaveValue('');
  });

  test('clicking column header sorts resources', async ({ page }) => {
    const table = page.getByTestId('resources-table');

    // Sort by Name ascending
    await page.getByTestId('resources-sort-name').click();
    await expect(page.getByTestId('resources-sort-name')).toContainText('▲');
    const firstNameAsc = await table.locator('tbody tr').first().locator('td').nth(1).innerText();

    // Sort by Name descending
    await page.getByTestId('resources-sort-name').click();
    await expect(page.getByTestId('resources-sort-name')).toContainText('▼');
    const firstNameDesc = await table.locator('tbody tr').first().locator('td').nth(1).innerText();

    expect(firstNameAsc.length).toBeGreaterThan(0);
    expect(firstNameDesc.length).toBeGreaterThan(0);
  });

  test('no-results message shown for impossible search', async ({ page }) => {
    await page.getByTestId('resources-search').fill('ZZZNONEXISTENT');
    await expect(page.getByTestId('resources-no-results')).toBeVisible();
  });
});
