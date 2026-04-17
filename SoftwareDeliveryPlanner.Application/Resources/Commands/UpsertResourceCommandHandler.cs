using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

internal sealed class UpsertResourceCommandHandler : IRequestHandler<UpsertResourceCommand, Result>
{
    private readonly IResourceOrchestrator _orchestrator;

    public UpsertResourceCommandHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(UpsertResourceCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.UpsertResourceAsync(
            request.Id, request.ResourceId, request.ResourceName,
            request.Role, request.Team, request.AvailabilityPct,
            request.DailyCapacity, request.StartDate, request.Active,
            request.Notes, request.IsNew, cancellationToken);

        return Result.Success();
    }
}

internal sealed class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, Result>
{
    private readonly IResourceOrchestrator _orchestrator;

    public DeleteResourceCommandHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteResourceAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
