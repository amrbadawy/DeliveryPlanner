using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

public sealed class AddAdjustmentCommandHandler : IRequestHandler<AddAdjustmentCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public AddAdjustmentCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(AddAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = new Adjustment
        {
            ResourceId = request.ResourceId,
            AdjType = request.AdjType,
            AvailabilityPct = request.AvailabilityPct,
            AdjStart = request.AdjStart,
            AdjEnd = request.AdjEnd,
            Notes = request.Notes
        };

        await _orchestrator.AddAdjustmentAsync(adjustment, cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteAdjustmentCommandHandler : IRequestHandler<DeleteAdjustmentCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public DeleteAdjustmentCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteAdjustmentCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteAdjustmentAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
