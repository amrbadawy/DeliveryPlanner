using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Queries;

internal sealed class GetScenariosQueryHandler : IRequestHandler<GetScenariosQuery, Result<List<PlanScenario>>>
{
    private readonly IScenarioOrchestrator _orchestrator;

    public GetScenariosQueryHandler(IScenarioOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<PlanScenario>>> Handle(GetScenariosQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetScenariosAsync();
}
