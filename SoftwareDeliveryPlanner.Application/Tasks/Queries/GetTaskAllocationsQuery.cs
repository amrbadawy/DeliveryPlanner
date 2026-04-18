using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Queries;

public sealed record TaskAllocationDto(
    string ResourceId,
    string ResourceName,
    string Role,
    string Team,
    double AvailabilityPct,
    DateTime? StartDate,
    DateTime? EndDate);

public sealed record GetTaskAllocationsQuery(string TaskId) : IRequest<Result<List<TaskAllocationDto>>>;

internal sealed class GetTaskAllocationsQueryHandler : IRequestHandler<GetTaskAllocationsQuery, Result<List<TaskAllocationDto>>>
{
    private readonly IPlanningQueryService _planningQueryService;

    public GetTaskAllocationsQueryHandler(IPlanningQueryService planningQueryService)
        => _planningQueryService = planningQueryService;

    public async Task<Result<List<TaskAllocationDto>>> Handle(GetTaskAllocationsQuery request, CancellationToken cancellationToken)
        => await _planningQueryService.GetTaskAllocationsAsync(request.TaskId, cancellationToken);
}
