namespace SoftwareDeliveryPlanner.Application.Abstractions;

/// <summary>
/// Composite interface that aggregates all orchestrator concerns.
/// Prefer injecting the focused interface (e.g. <see cref="ITaskOrchestrator"/>)
/// when a consumer only needs a subset of operations.
/// </summary>
public interface ISchedulingOrchestrator
    : ISchedulerService,
      ITaskOrchestrator,
      IResourceOrchestrator,
      IAdjustmentOrchestrator,
      IHolidayOrchestrator,
      IPlanningQueryService
{
}
