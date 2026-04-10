# Phase 1 - DDD + CQRS Foundation

This phase introduces the minimum viable architecture split and keeps user-facing behavior stable.

## Delivered in Phase 1

- Added solution-level projects:
  - `SoftwareDeliveryPlanner.Domain`
  - `SoftwareDeliveryPlanner.Application`
  - `SoftwareDeliveryPlanner.Infrastructure`
  - `SoftwareDeliveryPlanner.ArchTests`
- Moved current persistence entities and scheduling service into isolated projects while keeping namespaces stable for compatibility.
- Added shared value object standards in `Domain`:
  - `TaskId`
  - `ResourceId`
  - `Percentage`
  - `DateRange`
- Added CQRS baseline with MediatR (Apache-2.0 compatible line):
  - `RunSchedulerCommand`
  - `GetDashboardKpisQuery`
- Added infrastructure adapter (`ISchedulingOrchestrator`) to decouple application handlers from EF Core implementation details.
- Migrated dashboard page to command/query handlers through `IMediator`.
- Added architecture dependency tests using NetArchTest.
- Added automated license policy gate for NuGet + npm.
- Added CI steps for architecture tests, license checks, and Playwright smoke.

## Dependency Intent

- `Domain`: no dependency on `Application`, `Infrastructure`, or `Blazor`.
- `Application`: depends on `Domain` only.
- `Infrastructure`: depends on `Application` and `Domain`.
- `Blazor`: depends on `Application` and `Infrastructure` through composition root.

## Notes

- Phase 1 preserves behavior intentionally; it is an extraction and seam-creation step.
- Existing page-level CRUD flows remain in place except dashboard CQRS path.
- Scheduler implementation is unchanged functionally and moved to infrastructure for later decomposition.

## Performance Guardrail

- Baseline smoke test and core unit tests are required to remain green.
- Scheduler/runtime logic was not rewritten in this phase to avoid performance regressions.

## Next Phase Entry Criteria

Phase 2 can start when:

- Architecture tests remain stable in CI.
- License verification remains green.
- Dashboard command/query path is stable in production-like runs.
