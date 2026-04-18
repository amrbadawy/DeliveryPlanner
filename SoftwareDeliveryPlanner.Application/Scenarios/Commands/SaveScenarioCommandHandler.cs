using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

internal sealed class SaveScenarioCommandHandler : IRequestHandler<SaveScenarioCommand, Result>
{
    private readonly ISchedulerService _schedulerService;
    private readonly IScenarioOrchestrator _orchestrator;
    private readonly TimeProvider _timeProvider;

    public SaveScenarioCommandHandler(ISchedulerService schedulerService, IScenarioOrchestrator orchestrator, TimeProvider timeProvider)
    {
        _schedulerService = schedulerService;
        _orchestrator = orchestrator;
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
            null,
            kpis.OverallFinish,
            kpis.TotalEstimation,
            request.Notes,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _orchestrator.SaveScenarioAsync(scenario);
        return Result.Success();
    }
}
