using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Queries;

internal sealed class GetScenarioDetailQueryHandler : IRequestHandler<GetScenarioDetailQuery, Result<PlanScenario>>
{
    private readonly IScenarioOrchestrator _orchestrator;

    public GetScenarioDetailQueryHandler(IScenarioOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<PlanScenario>> Handle(GetScenarioDetailQuery request, CancellationToken cancellationToken)
    {
        var scenario = await _orchestrator.GetScenarioWithSnapshotsAsync(request.ScenarioId);

        if (scenario is null)
            return new Error("Scenario.NotFound", $"Scenario with ID {request.ScenarioId} was not found.");

        return scenario;
    }
}
