using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Resources.Commands;

public sealed class UpsertResourceCommandHandler : IRequestHandler<UpsertResourceCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public UpsertResourceCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(UpsertResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = new TeamMember
        {
            Id = request.Id,
            ResourceId = request.ResourceId,
            ResourceName = request.ResourceName,
            Role = request.Role,
            Team = request.Team,
            AvailabilityPct = request.AvailabilityPct,
            DailyCapacity = request.DailyCapacity,
            StartDate = request.StartDate,
            Active = request.Active,
            Notes = request.Notes
        };

        await _orchestrator.UpsertResourceAsync(resource, request.IsNew, cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, Unit>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public DeleteResourceCommandHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Unit> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.DeleteResourceAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
