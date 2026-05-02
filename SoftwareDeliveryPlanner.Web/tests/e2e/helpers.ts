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
    // Try matching by visible label first; use a short timeout so we fall through quickly
    // to a value-based match when the caller passes a code/key rather than a display name.
    await element.selectOption({ label: value }, { timeout: 1_000 }).catch(async () => {
      await element.selectOption({ value });
    });
    await page.waitForTimeout(150);
    return;
  }

  await element.fill(value);
  await element.blur();
  await page.waitForTimeout(150);
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

  // Some scenarios set a default saved view that auto-applies on load.
  // Test setup should start from an unfiltered baseline unless a test explicitly
  // opts into a saved/default view flow.
  const clearAll = page.getByTestId('task-filter-clear-all');
  if (await clearAll.count()) {
    await clearAll.click();
  }
}

export async function countRowsByText(table: Locator, text: string): Promise<number> {
  return table.locator('tbody tr', { hasText: text }).count();
}

// ─────────────────────────────────────────────────────────────────────────────
// Gantt-phase-2 helpers — UI-driven seeding + fault injection.
//
// These helpers use only the production UI surface (testid attributes already
// present in production code) plus the env-gated test-fault endpoints exposed
// when SDP_TEST_FAULTS=1. There are no hidden test-only DB endpoints.
// ─────────────────────────────────────────────────────────────────────────────

const TEST_FAULTS_BASE = 'http://localhost:2026';

/**
 * Arms a server-side test fault for a specific operation key (e.g. "GanttSegments").
 * Subsequent requests that pass through the corresponding handler will throw
 * a TestInjectedFaultException, which the page surfaces as its error UI.
 *
 * The endpoint is only registered when SDP_TEST_FAULTS=1; this helper will
 * silently fail (and log a console hint) if the env var was not set in
 * playwright.config.ts so the spec author can diagnose quickly.
 */
export async function triggerTestFault(page: Page, operation: string): Promise<void> {
  const ctx = page.context();
  const response = await ctx.request.post(
    `${TEST_FAULTS_BASE}/test-faults/arm?operation=${encodeURIComponent(operation)}`,
  );
  if (!response.ok()) {
    throw new Error(
      `triggerTestFault('${operation}') failed: ${response.status()} ${response.statusText()}. ` +
        `Did you set SDP_TEST_FAULTS=1 in playwright.config.ts webServer.env?`,
    );
  }
}

/**
 * Clears a previously-armed fault. Pass no operation to clear every armed fault.
 * Always safe to call from afterEach for cleanup.
 */
export async function clearTestFault(page: Page, operation?: string): Promise<void> {
  const ctx = page.context();
  const url = operation
    ? `${TEST_FAULTS_BASE}/test-faults/clear?operation=${encodeURIComponent(operation)}`
    : `${TEST_FAULTS_BASE}/test-faults/clear`;
  // Don't fail tests on cleanup; just log and continue.
  await ctx.request.post(url).catch(() => {
    /* ignore — cleanup must never fail the test */
  });
}

/**
 * Persists a Settings page value via the production UI. Driver-friendly wrapper
 * around the strategy / week-numbering / scenario-zoom save flows. Uses the
 * stable testid contract: settings-{key}-select + settings-{key}-save.
 *
 * key: one of "strategy", "week-numbering", "scenario-gantt-zoom".
 */
export async function setSetting(page: Page, key: string, value: string): Promise<void> {
  await gotoPage(page, '/settings');
  const select = page.getByTestId(`settings-${key}-select`);
  await expect(select).toBeVisible();
  await select.selectOption({ value });
  await page.getByTestId(`settings-${key}-save`).click();
  await expect(page.getByTestId('settings-status')).toBeVisible({ timeout: 5_000 });
}

/**
 * Runs the scheduler via the Tasks refresh button (the production trigger),
 * waits for the tasks table to populate, then optionally returns the row count.
 * Identical to runSchedulerFromDashboard but with a clearer name for Gantt specs.
 */
export async function runScheduler(page: Page): Promise<number> {
  await runSchedulerFromDashboard(page);
  const table = page.getByTestId('tasks-table');
  return table.locator('tbody tr').count();
}

/**
 * Marks every scheduled task as hidden via the persistent task filter sidebar
 * (Phase 4 bulk-hide). Resulting Gantt should show its empty state because no
 * scheduled tasks remain visible.
 *
 * Caller must navigate to /tasks first; this helper does NOT navigate.
 */
export async function hideTaskViaSidebar(page: Page, taskId: string): Promise<void> {
  const row = page.getByTestId(`tasks-row-${taskId}`);
  await expect(row).toBeVisible();
  const hideBtn = row.getByTestId(`tasks-row-hide-${taskId}`);
  if (await hideBtn.count()) {
    await hideBtn.click();
  }
}

/**
 * Returns the bounding rect of a Gantt bar identified by task id, or null if
 * the bar is not currently rendered (filtered out, scrolled out, etc.).
 *
 * Used by geometry-sensitive specs (today line, segment alignment, zoom width).
 */
export async function getBarBoundingRect(
  page: Page,
  taskId: string,
): Promise<{ x: number; y: number; width: number; height: number } | null> {
  const bar = page.getByTestId(`gantt-bar-${taskId}`);
  if (!(await bar.count())) return null;
  const box = await bar.boundingBox();
  return box;
}

/**
 * Unschedules all tasks by setting plan_start_date to a far-future date and
 * re-running the scheduler. Subsequent Gantt page loads should render the
 * empty state since no task can fit in the past plan window.
 *
 * Faster than deleting rows; deterministic; uses only production endpoints.
 */
export async function unscheduleAllTasks(page: Page): Promise<void> {
  // The cheapest route: open the Tasks page with no schedulable tasks. Achieved
  // by hiding everything via bulk filter "Hide all" (Phase 4 bulk action).
  await gotoPage(page, '/tasks');
  const selectAll = page.getByTestId('tasks-select-all');
  if (await selectAll.count()) {
    await selectAll.check();
    const bulkHide = page.getByTestId('tasks-bulk-hide');
    if (await bulkHide.count()) {
      await bulkHide.click();
    }
  }
}
