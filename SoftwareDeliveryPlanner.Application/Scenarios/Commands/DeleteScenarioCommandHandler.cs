using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

internal sealed class DeleteScenarioCommandHandler : IRequestHandler<DeleteScenarioCommand, Result>
{
    private readonly IScenarioOrchestrator _orchestrator;

    public DeleteScenarioCommandHandler(IScenarioOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteScenarioCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteScenarioAsync(request.Id);
        return Result.Success();
    }
}
