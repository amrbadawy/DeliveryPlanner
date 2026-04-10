import { expect, type Locator, type Page } from '@playwright/test';

export async function gotoPage(page: Page, path: string): Promise<void> {
  await page.goto(path, { waitUntil: 'networkidle' });
}

export async function waitForTableRows(table: Locator, minimum = 1): Promise<number> {
  await expect(table).toBeVisible();
  await expect.poll(async () => await table.locator('tbody tr').count(), {
    message: 'Wait for table rows',
    timeout: 15_000,
  }).toBeGreaterThanOrEqual(minimum);
  return table.locator('tbody tr').count();
}

export async function expectModalVisible(page: Page, testId: string): Promise<void> {
  await expect(page.getByTestId(testId)).toBeVisible();
}

export function uniqueSuffix(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
}

export async function fillInputByTestId(page: Page, testId: string, value: string): Promise<void> {
  const element = page.getByTestId(testId);
  await expect(element).toBeVisible();

  const tagName = await element.evaluate((el) => el.tagName.toLowerCase());
  if (tagName === 'select') {
    await element.selectOption({ label: value });
    return;
  }

  await element.fill(value);
}

export async function runSchedulerFromDashboard(page: Page): Promise<void> {
  // More reliable for setup: trigger scheduler from Tasks refresh.
  await gotoPage(page, '/tasks');
  const refreshBtn = page.getByTestId('tasks-refresh');
  await expect(refreshBtn).toBeVisible();
  await refreshBtn.click();

  const table = page.getByTestId('tasks-table');
  await expect(table).toBeVisible();
  await expect.poll(async () => await table.locator('tbody tr').count(), {
    message: 'Wait tasks table after scheduler run',
    timeout: 20_000,
  }).toBeGreaterThan(0);
}

export async function countRowsByText(table: Locator, text: string): Promise<number> {
  return table.locator('tbody tr', { hasText: text }).count();
}
