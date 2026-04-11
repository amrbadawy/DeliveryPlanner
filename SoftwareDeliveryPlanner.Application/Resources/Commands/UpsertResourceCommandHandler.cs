using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class UpsertResourceCommandHandler : IRequestHandler<UpsertResourceCommand, Unit>
{
    private readonly IResourceOrchestrator _orchestrator;

    public UpsertResourceCommandHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(UpsertResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = TeamMember.Create(
            request.ResourceId,
            request.ResourceName,
            request.Role,
            request.Team,
            request.AvailabilityPct,
            request.DailyCapacity,
            request.StartDate,
            request.Active,
            request.Notes);

        resource.Id = request.Id;

        await _orchestrator.UpsertResourceAsync(resource, request.IsNew, cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, Unit>
{
    private readonly IResourceOrchestrator _orchestrator;

    public DeleteResourceCommandHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteResourceAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
