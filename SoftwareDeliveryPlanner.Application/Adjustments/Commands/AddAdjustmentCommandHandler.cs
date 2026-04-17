using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Adjustments.Commands;

internal sealed class AddAdjustmentCommandHandler : IRequestHandler<AddAdjustmentCommand, Result>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public AddAdjustmentCommandHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(AddAdjustmentCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.AddAdjustmentAsync(
            request.ResourceId, request.AdjType, request.AvailabilityPct,
            request.AdjStart, request.AdjEnd, request.Notes,
            cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeleteAdjustmentCommandHandler : IRequestHandler<DeleteAdjustmentCommand, Result>
{
    private readonly IAdjustmentOrchestrator _orchestrator;

    public DeleteAdjustmentCommandHandler(IAdjustmentOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteAdjustmentCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteAdjustmentAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
