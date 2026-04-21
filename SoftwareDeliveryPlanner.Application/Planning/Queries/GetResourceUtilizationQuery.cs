using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record ResourceUtilizationDto(
    string ResourceId,
    string ResourceName,
    string Role,
    double TotalAvailableHours,
    double TotalAllocatedHours,
    double UtilizationPct,
    int BenchDays,
    int OverallocatedDays);

public sealed record GetResourceUtilizationQuery() : IRequest<Result<List<ResourceUtilizationDto>>>;

internal sealed class GetResourceUtilizationQueryHandler : IRequestHandler<GetResourceUtilizationQuery, Result<List<ResourceUtilizationDto>>>
{
    private readonly IPlanningQueryService _service;

    public GetResourceUtilizationQueryHandler(IPlanningQueryService service)
        => _service = service;

    public async Task<Result<List<ResourceUtilizationDto>>> Handle(GetResourceUtilizationQuery request, CancellationToken cancellationToken)
        => await _service.GetResourceUtilizationAsync(cancellationToken);
}
