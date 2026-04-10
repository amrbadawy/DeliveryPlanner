using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Application.Resources.Queries;

public sealed class GetResourcesQueryHandler : IRequestHandler<GetResourcesQuery, List<TeamMember>>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetResourcesQueryHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<List<TeamMember>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetResourcesAsync(cancellationToken);
}

public sealed class GetResourceCountQueryHandler : IRequestHandler<GetResourceCountQuery, int>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetResourceCountQueryHandler(ISchedulingOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<int> Handle(GetResourceCountQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetResourceCountAsync(cancellationToken);
}
