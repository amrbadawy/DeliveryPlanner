using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record GetFeasibilityQuery(string? TaskId = null) : IRequest<Result<List<FeasibilityResultDto>>>;

internal sealed class GetFeasibilityQueryHandler(IPlanningQueryService service)
    : IRequestHandler<GetFeasibilityQuery, Result<List<FeasibilityResultDto>>>
{
    public async Task<Result<List<FeasibilityResultDto>>> Handle(GetFeasibilityQuery request, CancellationToken cancellationToken)
    {
        var results = await service.GetFeasibilityAsync(request.TaskId, cancellationToken);
        return Result<List<FeasibilityResultDto>>.Success(results);
    }
}
