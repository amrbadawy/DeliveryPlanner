using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

internal sealed class AddAdjustmentCommandHandler : IRequestHandler<AddAdjustmentCommand, Unit>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public AddAdjustmentCommandHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(AddAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = Adjustment.Create(
            request.ResourceId,
            request.AdjType,
            request.AvailabilityPct,
            request.AdjStart,
            request.AdjEnd,
            request.Notes);

        await _orchestrator.AddAdjustmentAsync(adjustment, cancellationToken);
        return Unit.Value;
    }
}

internal sealed class DeleteAdjustmentCommandHandler : IRequestHandler<DeleteAdjustmentCommand, Unit>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public DeleteAdjustmentCommandHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteAdjustmentCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteAdjustmentAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
