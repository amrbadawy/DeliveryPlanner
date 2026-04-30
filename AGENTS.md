# AGENTS.md — Software Delivery Planner

This file provides OpenCode with the context needed to work effectively in this codebase.

---

## Project Overview

**Software Delivery Planner** is a Blazor Server application for managing software delivery schedules, tasks, resources, holidays, and Gantt chart visualisation.

- **Architecture:** Domain-Driven Design (DDD) + CQRS (via MediatR)
- **Runtime:** .NET 10 / C#
- **Frontend:** Blazor Server Components + Bootstrap
- **Database:** SQL Server (EF Core 10) in production; SQLite (isolated per test run) in e2e tests
- **Validation:** FluentValidation 12
- **Unit/Arch Tests:** xUnit
- **E2E Tests:** Playwright (`@playwright/test ^1.59.1`) with TypeScript

---

## Solution Structure

```
SoftwareDeliveryPlanner.slnx
├── SoftwareDeliveryPlanner.Domain/          # Entities, value objects, domain events
├── SoftwareDeliveryPlanner.SharedKernel/    # Base classes, shared abstractions
├── SoftwareDeliveryPlanner.Application/    # CQRS commands/queries (MediatR), validators
├── SoftwareDeliveryPlanner.Infrastructure/ # EF Core DbContext, repositories, migrations
├── SoftwareDeliveryPlanner.Web/            # Blazor Server app (entry point)
│   ├── Components/                         # Blazor components (.razor)
│   ├── tests/e2e/                          # Playwright e2e specs
│   ├── playwright.config.ts
│   └── package.json
├── SoftwareDeliveryPlanner.Tests/          # xUnit unit tests
└── SoftwareDeliveryPlanner.ArchTests/      # NetArchTest architecture rule tests
```

---

## Building & Running

### Run the app locally

```powershell
# From repo root
dotnet run --project SoftwareDeliveryPlanner.Web/SoftwareDeliveryPlanner.Web.csproj --urls "http://localhost:2026"
```

Or use the convenience scripts in `.scripts/`:

```powershell
.scripts\kill-and-run.ps1       # Kill any existing instance, then start fresh
.scripts\run-only.ps1           # Start without killing
.scripts\reset-kill-and-run.ps1 # Reset DB + restart
.scripts\kill-only.ps1          # Kill only
```

### Build the full solution

```powershell
dotnet build SoftwareDeliveryPlanner.slnx
```

### Run unit and architecture tests

```powershell
dotnet test SoftwareDeliveryPlanner.Tests
dotnet test SoftwareDeliveryPlanner.ArchTests
```

---

## E2E Testing with Playwright

### Location

All e2e specs live in:
```
SoftwareDeliveryPlanner.Web/tests/e2e/
```

### npm scripts (run from `SoftwareDeliveryPlanner.Web/`)

| Script | Command | Purpose |
|--------|---------|---------|
| `test:e2e` | `playwright test` | Full suite |
| `test:e2e:smoke` | `playwright test tests/e2e/smoke.spec.ts` | Smoke only (also used in CI) |
| `test:e2e:ui` | `playwright test --ui` | Interactive Playwright UI |
| `test:e2e:headed` | `playwright test --headed` | Headed browser (visible) |
| `test:e2e:report` | `playwright show-report` | Open last HTML report |

### Key Playwright config facts

- **Base URL:** `http://localhost:2026`
- **Browser:** Chromium only (Desktop Chrome)
- **Parallelism:** Serial (`workers: 1`, `fullyParallel: false`)
- **Retries:** 2 on CI, 0 locally
- **Artifacts on failure:** screenshot, video, trace
- **Viewport:** 1440×900
- **Web server:** Started automatically by Playwright before tests run
- **Test DB:** Isolated SQLite at `.playwright/planner-e2e.db` — injected via `PLANNER_DB_PATH` env var

### Existing spec files

| File | Coverage area |
|------|--------------|
| `smoke.spec.ts` | App loads, critical navigation, modal entry points |
| `navigation-and-dashboard.spec.ts` | All pages navigate, scheduler runs |
| `tasks.spec.ts` | Tasks CRUD + edge cases |
| `task-details.spec.ts` | Task detail page, notes CRUD, assigned resources |
| `task-dependencies.spec.ts` | Dependency multi-select and badge display |
| `resources.spec.ts` | Resources CRUD + edge cases |
| `holidays.spec.ts` | Holidays CRUD, overlap validation, date logic |
| `adjustments.spec.ts` | Adjustments CRUD + edge cases |
| `scenarios.spec.ts` | What-if scenarios save, view, compare, delete |
| `gantt.spec.ts` | Gantt chart rendering, bars, legend, refresh |
| `heatmap.spec.ts` | Workload heatmap table, legend after scheduler run |
| `search-filter-sort.spec.ts` | Search, filter, sort for tasks and resources |
| `validation.spec.ts` | Form validation across all entities |
| `calendar-timeline-output.spec.ts` | Calendar, timeline, CSV export |
| `dashboard-features.spec.ts` | Stale-plan warning, risk trend chart, KPI cards |
| `layout-features.spec.ts` | Auto-schedule toggle, command palette, critical path |
| `audit-log.spec.ts` | Activity/audit log page loads and refresh |
| `bulk-import.spec.ts` | Bulk CSV import modal open/close |
| `drag-drop-priority.spec.ts` | Drag-and-drop task priority reordering |
| `task-filter-sidebar.spec.ts` | Persistent left filter sidebar — chips, search, URL sync, per-page scope |
| `task-filter-saved-views.spec.ts` | Saved views CRUD, pin/hide row actions, ghost-dependency stub indicator on Gantt |

### Shared test utilities

**`helpers.ts`** — import from here in all new specs:

| Function | Purpose |
|----------|---------|
| `gotoPage(page, path)` | Navigate and wait for `networkidle` |
| `waitForTableRows(table, min?)` | Poll until table has ≥ N rows (15s timeout) |
| `expectModalVisible(page, testId)` | Assert a modal is visible by `data-testid` |
| `uniqueSuffix(prefix)` | Generate unique string for test data (timestamp + random) |
| `fillInputByTestId(page, testId, value)` | Smart fill for `<input>` and `<select>` by testid |
| `runSchedulerFromDashboard(page)` | Navigate to /tasks, click refresh, wait for rows |
| `countRowsByText(table, text)` | Count tbody rows containing specific text |

**`db-assertions.ts`** — SQLite DB assertion helpers for verifying persisted data directly.

### Selector convention

All interactive elements use `data-testid` attributes. Always prefer:
```typescript
page.getByTestId('some-test-id')
```
over CSS selectors or text-based selectors.

### Writing new specs — checklist

1. Import from `helpers.ts` and `@playwright/test`
2. Use `uniqueSuffix()` for any test data names to avoid collisions
3. Use `fillInputByTestId()` — it handles both `<input>` and `<select>`
4. Use `waitForTableRows()` after any operation that populates a table
5. Use `expectModalVisible()` to confirm modals open before interacting
6. Always `await` navigation with `gotoPage()` (ensures `networkidle`)
7. Clean up test data in `afterEach` or use the isolated SQLite DB (reset per run)

---

## OpenCode + Playwright MCP — AI-Powered Testing

The Playwright MCP server is configured in `opencode.jsonc`. It is disabled globally and only active in the **`tester` agent** to avoid token bloat.

### When to use the tester agent

Invoke it when you want OpenCode to:
- Browse the live app to understand UI before writing a test
- Take screenshots to verify page state
- Interactively debug a failing spec
- Explore new UI and auto-generate a spec from observed behaviour

### Key MCP tools available

| Tool | What it does |
|------|-------------|
| `playwright_navigate` | Go to a URL |
| `playwright_screenshot` | Capture current page state |
| `playwright_click` | Click an element |
| `playwright_fill` | Type into an input |
| `playwright_evaluate` | Run JavaScript in the browser |
| `playwright_get_text` | Extract visible text from the page |

### Example prompts for the tester agent

```
Navigate to http://localhost:2026/tasks, open the add modal, and write a
Playwright spec that covers the happy path for adding a new task.

Take a screenshot of the Gantt page and write a spec that verifies the
chart renders with at least one bar after running the scheduler.

Debug why gantt.spec.ts is failing — navigate to the page and inspect
what's actually rendered.
```

> **Important:** The app must be running at `http://localhost:2026` before using
> MCP tools interactively. Use `.scripts\run-only.ps1` to start it.

---

## Architecture Rules (enforced by ArchTests)

- `Domain` must not reference `Application`, `Infrastructure`, or `Web`
- `Application` must not reference `Infrastructure` or `Web`
- `Infrastructure` depends on `Application` and `Domain`
- `Web` is the composition root — it can reference all layers
- Commands and Queries must implement MediatR `IRequest<T>`
- Validators must implement `AbstractValidator<T>` (FluentValidation)

---

## CI Pipeline (GitHub Actions)

Runs on push/PR to `main` / `master` on `windows-latest`:

1. `dotnet restore`
2. `dotnet build` (Release)
3. `dotnet test SoftwareDeliveryPlanner.Tests`
4. `dotnet test SoftwareDeliveryPlanner.ArchTests`
5. Node 22 setup + `npm ci`
6. License policy verification (`.github/scripts/verify-licenses.ps1`)
7. `npm run test:e2e:smoke` — **only smoke spec runs in CI**

The full e2e suite is intended to be run locally before merging.
