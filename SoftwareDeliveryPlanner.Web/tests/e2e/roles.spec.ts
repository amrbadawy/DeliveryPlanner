import { test, expect } from '@playwright/test';
import { expectModalVisible, fillInputByTestId, gotoPage } from './helpers';

test.describe('Roles CRUD + constraints', () => {
  test('add, edit, block delete when in use, then delete after unassign', async ({ page }) => {
    await gotoPage(page, '/roles');

    const roleCode = `E2E-ROLE-${Date.now()}`;
    const roleName = 'E2E Role';
    const roleNameUpdated = 'E2E Role Updated';

    await page.getByTestId('roles-add').click();
    await expectModalVisible(page, 'roles-modal');
    await fillInputByTestId(page, 'roles-code', roleCode);
    await fillInputByTestId(page, 'roles-display-name', roleName);
    await fillInputByTestId(page, 'roles-sort-order', '99');
    await page.getByTestId('roles-save').click();

    await expect(page.getByTestId('roles-modal')).toBeHidden();
    await expect(page.getByTestId(`roles-row-${roleCode}`)).toContainText(roleName);

    await page.getByTestId(`roles-edit-${roleCode}`).click();
    await expectModalVisible(page, 'roles-modal');
    await fillInputByTestId(page, 'roles-display-name', roleNameUpdated);
    await page.getByTestId('roles-save').click();
    await expect(page.getByTestId('roles-modal')).toBeHidden();
    await expect(page.getByTestId(`roles-row-${roleCode}`)).toContainText(roleNameUpdated);

    await gotoPage(page, '/resources');
    await page.getByTestId('resources-add').click();
    await expectModalVisible(page, 'resources-modal');
    const resourceId = (await page.getByTestId('resources-id').inputValue()).trim();
    await fillInputByTestId(page, 'resources-name', `Role User ${Date.now()}`);
    await fillInputByTestId(page, 'resources-role', roleCode);
    await fillInputByTestId(page, 'resources-team', 'E2E Team');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();
    await expect(page.getByTestId(`resources-row-${resourceId}`)).toContainText(roleCode);

    await gotoPage(page, '/roles');
    await page.getByTestId(`roles-delete-${roleCode}`).click();
    await expectModalVisible(page, 'roles-delete-modal');
    await page.getByTestId('roles-delete-modal-confirm').click();
    await expect(page.getByTestId('roles-toast')).toContainText('Cannot delete a role');
    await expect(page.getByTestId(`roles-row-${roleCode}`)).toBeVisible();

    await gotoPage(page, '/resources');
    await page.getByTestId(`resources-edit-${resourceId}`).click();
    await expectModalVisible(page, 'resources-modal');
    await fillInputByTestId(page, 'resources-role', 'Developer');
    await page.getByTestId('resources-save').click();
    await expect(page.getByTestId('resources-modal')).toBeHidden();

    await gotoPage(page, '/roles');
    await page.getByTestId(`roles-delete-${roleCode}`).click();
    await expectModalVisible(page, 'roles-delete-modal');
    await page.getByTestId('roles-delete-modal-confirm').click();
    await expect(page.getByTestId('roles-delete-modal')).toBeHidden();
    await expect(page.getByTestId(`roles-row-${roleCode}`)).toHaveCount(0);
  });
});
