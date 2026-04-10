import { test, expect } from '@playwright/test';
import {
  countRowsByText,
  expectModalVisible,
  fillInputByTestId,
  gotoPage,
  uniqueSuffix,
  waitForTableRows,
} from './helpers';
import { countTasksByTaskId, getTaskByTaskId } from './db-assertions';

test.describe('Tasks CRUD + edge cases', () => {
  test('add, edit, refresh, and delete task', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('E2E Task');
    const updatedName = `${serviceName} Updated`;

    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');

    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await fillInputByTestId(page, 'tasks-dev-estimation', '4');
    await fillInputByTestId(page, 'tasks-max-dev', '2');
    await fillInputByTestId(page, 'tasks-priority', '3');
    await fillInputByTestId(page, 'tasks-strict-date', '2026-12-10');

    const taskId = (await page.getByTestId('tasks-task-id').inputValue()).trim();
    expect(taskId).toMatch(/^SVC-\d{3,}$/);

    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: serviceName })).toHaveCount(1);
    expect(countTasksByTaskId(taskId)).toBe(1);
    expect(getTaskByTaskId(taskId)?.serviceName).toBe(serviceName);

    await page.getByTestId(`tasks-edit-${taskId}`).click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', updatedName);
    await fillInputByTestId(page, 'tasks-dev-estimation', '6');
    await page.getByTestId('tasks-save').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);
    const updatedTask = getTaskByTaskId(taskId);
    expect(updatedTask).not.toBeNull();
    expect(updatedTask?.serviceName).toBe(updatedName);
    expect(updatedTask?.devEstimation).toBe(6);

    await page.getByTestId('tasks-refresh').click();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(1);

    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-confirm').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();
    await expect(table.locator('tbody tr', { hasText: updatedName })).toHaveCount(0);
    expect(countTasksByTaskId(taskId)).toBe(0);
  });

  test('cancel add and ensure no row added', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const serviceName = uniqueSuffix('E2E Task Cancel');

    await page.getByTestId('tasks-add').click();
    await expectModalVisible(page, 'tasks-modal');
    await fillInputByTestId(page, 'tasks-service-name', serviceName);
    await page.getByTestId('tasks-cancel').click();
    await expect(page.getByTestId('tasks-modal')).toBeHidden();

    const count = await countRowsByText(table, serviceName);
    expect(count).toBe(0);
  });

  test('cancel delete keeps row intact', async ({ page }) => {
    await gotoPage(page, '/tasks');
    const table = page.getByTestId('tasks-table');
    await waitForTableRows(table);

    const firstRow = table.locator('tbody tr').first();
    const taskId = (await firstRow.locator('td').nth(0).innerText()).trim();
    const serviceName = (await firstRow.locator('td').nth(1).innerText()).trim();

    await page.getByTestId(`tasks-delete-${taskId}`).click();
    await expectModalVisible(page, 'tasks-delete-modal');
    await page.getByTestId('tasks-delete-cancel').click();
    await expect(page.getByTestId('tasks-delete-modal')).toBeHidden();

    await expect(table.locator('tbody tr', { hasText: serviceName })).toHaveCount(1);
    expect(countTasksByTaskId(taskId)).toBe(1);
  });
});
