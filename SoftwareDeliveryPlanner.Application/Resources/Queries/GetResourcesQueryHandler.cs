using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Resources.Queries;

internal sealed class GetResourcesQueryHandler : IRequestHandler<GetResourcesQuery, Result<List<TeamMember>>>
{
    private readonly IResourceOrchestrator _orchestrator;

    public GetResourcesQueryHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<TeamMember>>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetResourcesAsync(cancellationToken);
}

internal sealed class GetResourceCountQueryHandler : IRequestHandler<GetResourceCountQuery, Result<int>>
{
    private readonly IResourceOrchestrator _orchestrator;

    public GetResourceCountQueryHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<int>> Handle(GetResourceCountQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetResourceCountAsync(cancellationToken);
}

internal sealed class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, Result<TeamMember?>>
{
    private readonly IResourceOrchestrator _orchestrator;

    public GetResourceByIdQueryHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<TeamMember?>> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetResourceByResourceIdAsync(request.ResourceId, cancellationToken);
}
