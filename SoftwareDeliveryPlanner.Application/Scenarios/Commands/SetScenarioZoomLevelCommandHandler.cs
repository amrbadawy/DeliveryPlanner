using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

internal sealed class SetScenarioZoomLevelCommandHandler : IRequestHandler<SetScenarioZoomLevelCommand, Result>
{
    private readonly IScenarioOrchestrator _orchestrator;

    public SetScenarioZoomLevelCommandHandler(IScenarioOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(SetScenarioZoomLevelCommand request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.SetScenarioZoomLevelAsync(request.ScenarioId, request.ZoomLevel, cancellationToken);
        return result is null
            ? Result.Failure(Error.NotFound("Scenario not found."))
            : Result.Success();
    }
}
