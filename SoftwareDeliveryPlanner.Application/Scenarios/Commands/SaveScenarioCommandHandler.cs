using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

internal sealed class SaveScenarioCommandHandler : IRequestHandler<SaveScenarioCommand, Result>
{
    private readonly ISchedulerService _schedulerService;
    private readonly IScenarioOrchestrator _orchestrator;
    private readonly ITaskOrchestrator _taskOrchestrator;
    private readonly TimeProvider _timeProvider;

    public SaveScenarioCommandHandler(
        ISchedulerService schedulerService,
        IScenarioOrchestrator orchestrator,
        ITaskOrchestrator taskOrchestrator,
        TimeProvider timeProvider)
    {
        _schedulerService = schedulerService;
        _orchestrator = orchestrator;
        _taskOrchestrator = taskOrchestrator;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SaveScenarioCommand request, CancellationToken cancellationToken)
    {
        var kpis = await _schedulerService.GetDashboardKpisAsync(cancellationToken);

        var scenario = PlanScenario.Create(
            request.ScenarioName,
            kpis.TotalServices,
            kpis.OnTrack,
            kpis.AtRisk,
            kpis.Late,
            kpis.Unscheduled,
            null,
            kpis.OverallFinish,
            kpis.TotalEstimation,
            request.Notes,
            _timeProvider.GetUtcNow().UtcDateTime);

        // Snapshot all tasks for historical Gantt chart view
        var tasks = await _taskOrchestrator.GetTasksAsync(cancellationToken);
        await _orchestrator.SaveScenarioWithSnapshotsAsync(scenario, tasks);

        return Result.Success();
    }
}
