using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Resources.Queries;

public sealed record ResourceFilterDto(
    string ResourceId,
    string ResourceName,
    string Role,
    string SeniorityLevel);

public sealed record GetResourcesForFilteringQuery : IRequest<Result<List<ResourceFilterDto>>>;

internal sealed class GetResourcesForFilteringQueryHandler : IRequestHandler<GetResourcesForFilteringQuery, Result<List<ResourceFilterDto>>>
{
    private readonly IResourceOrchestrator _orchestrator;

    public GetResourcesForFilteringQueryHandler(IResourceOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<ResourceFilterDto>>> Handle(GetResourcesForFilteringQuery request, CancellationToken cancellationToken)
    {
        var resources = await _orchestrator.GetResourcesAsync(cancellationToken);
        var result = resources
            .Select(r => new ResourceFilterDto(r.ResourceId, r.ResourceName, r.Role, r.SeniorityLevel ?? DomainConstants.Seniority.Mid))
            .OrderBy(r => r.ResourceName)
            .ToList();

        return Result<List<ResourceFilterDto>>.Success(result);
    }
}
