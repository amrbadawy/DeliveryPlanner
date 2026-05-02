import { test, expect } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * E2E coverage for Phase 4 — per-section bulk-action overflow menu.
 *
 * Each chip section (status, risk, priority, role, phase, dep) exposes a
 * three-dot trigger that opens a small dropdown with All / Clear / Invert.
 * These verify the menu opens, the actions toggle the chip selection state
 * as expected, and chip count badges render.
 */

test.describe('Filter sidebar — section bulk actions', () => {
  test.beforeEach(async ({ page }) => {
    await runSchedulerFromDashboard(page);
    await gotoPage(page, '/tasks');
  });

  test('bulk-menu trigger opens dropdown and "Clear" deselects all chips in section', async ({ page }) => {
    // Pre-select two status chips
    await page.getByTestId('task-filter-chip-status-not_started').click();
    await page.getByTestId('task-filter-chip-status-in_progress').click();

    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-status-in_progress')).toHaveAttribute('aria-pressed', 'true');

    // Open the bulk menu and click Clear
    await page.getByTestId('task-filter-bulk-trigger-status').click();
    await expect(page.getByTestId('task-filter-bulk-menu-status')).toBeVisible();
    await page.getByTestId('task-filter-bulk-clear-status').click();

    // Both chips should now be unpressed
    await expect(page.getByTestId('task-filter-chip-status-not_started')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('task-filter-chip-status-in_progress')).toHaveAttribute('aria-pressed', 'false');
  });

  test('"All" selects every chip in the section', async ({ page }) => {
    await page.getByTestId('task-filter-bulk-trigger-risk').click();
    await page.getByTestId('task-filter-bulk-all-risk').click();

    await expect(page.getByTestId('task-filter-chip-risk-on_track')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-risk-at_risk')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-risk-late')).toHaveAttribute('aria-pressed', 'true');
  });

  test('"Invert" flips the selection state of every chip in the section', async ({ page }) => {
    // Start with one priority selected
    await page.getByTestId('task-filter-chip-priority-high').click();
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'true');

    await page.getByTestId('task-filter-bulk-trigger-priority').click();
    await page.getByTestId('task-filter-bulk-invert-priority').click();

    // High should now be off, the others on
    await expect(page.getByTestId('task-filter-chip-priority-high')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.getByTestId('task-filter-chip-priority-medium')).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByTestId('task-filter-chip-priority-low')).toHaveAttribute('aria-pressed', 'true');
  });

  test('chip count badges render alongside chip labels', async ({ page }) => {
    // After running the scheduler, at least one status chip should show a count badge.
    const statusChip = page.getByTestId('task-filter-chip-status-not_started');
    const badge = statusChip.locator('.task-filter-chip-count');
    await expect(badge).toBeVisible();
    const text = (await badge.textContent())?.trim();
    expect(text).toMatch(/^\d+$/);
  });
});
