import { test, expect, Page } from '@playwright/test';
import { gotoPage, runSchedulerFromDashboard } from './helpers';

/**
 * Phase 3 Gantt: per-segment detail invariants.
 *
 * Existing gantt.spec.ts asserts that estimated and allocated segments are
 * "visually distinct". These tests pin down the exact contract:
 *   - estimated segments carry the `gantt-segment-estimated` CSS class
 *   - allocated segments do not carry that class
 *   - segment testid suffix is the role code in UPPER case (BA/SA/UX/UI/DEV/QA)
 *   - segment label text contains the role code, with a trailing "*" for
 *     estimated segments only
 */

async function ensureSegments(page: Page) {
  await runSchedulerFromDashboard(page);
  await gotoPage(page, '/gantt');
  const chart = page.getByTestId('gantt-chart');
  if (!(await chart.isVisible().catch(() => false))) {
    test.skip(true, 'Gantt chart not rendered (no scheduled tasks)');
  }
  const segs = chart.locator('[data-testid^="gantt-segment-"]');
  if ((await segs.count()) === 0) {
    test.skip(true, 'No segments rendered in this run');
  }
  return { chart, segs };
}

test.describe('Gantt segment detail', () => {
  test('every segment testid ends with an uppercase role code', async ({ page }) => {
    const { segs } = await ensureSegments(page);
    const ids = await segs.evaluateAll(els =>
      els.map(e => e.getAttribute('data-testid') ?? ''));
    const allowed = new Set(['BA', 'SA', 'UX', 'UI', 'DEV', 'QA']);
    for (const id of ids) {
      const role = id.split('-').pop() ?? '';
      expect(allowed.has(role), `segment ${id} role suffix not in pipeline set`).toBe(true);
    }
  });

  test('estimated segments carry gantt-segment-estimated class; allocated do not', async ({ page }) => {
    const { segs } = await ensureSegments(page);
    const data = await segs.evaluateAll(els => els.map(e => ({
      cls: e.className,
      label: (e.querySelector('.gantt-segment-label') as HTMLElement | null)?.innerText ?? '',
    })));
    if (data.length === 0) test.skip(true, 'no segments');

    for (const { cls, label } of data) {
      const isEstimatedClass = cls.includes('gantt-segment-estimated');
      const isEstimatedLabel = label.endsWith('*');
      expect(isEstimatedClass, `class/label mismatch: cls=${cls} label=${label}`)
        .toBe(isEstimatedLabel);
    }
  });

  test('segment label text contains the role code', async ({ page }) => {
    const { segs } = await ensureSegments(page);
    const data = await segs.evaluateAll(els => els.map(e => ({
      tid: e.getAttribute('data-testid') ?? '',
      label: (e.querySelector('.gantt-segment-label') as HTMLElement | null)?.innerText ?? '',
    })));
    for (const { tid, label } of data) {
      const role = tid.split('-').pop() ?? '';
      // label is `BA` or `BA*` — strip trailing star for comparison.
      const labelRole = label.replace(/\*$/, '');
      expect(labelRole, `segment ${tid} label "${label}" missing role`).toBe(role);
    }
  });

  test('segments use absolute positioning inside their bar', async ({ page }) => {
    const { segs } = await ensureSegments(page);
    const positions = await segs.evaluateAll(els =>
      els.map(e => getComputedStyle(e as HTMLElement).position));
    for (const pos of positions) {
      expect(pos).toBe('absolute');
    }
  });
});
