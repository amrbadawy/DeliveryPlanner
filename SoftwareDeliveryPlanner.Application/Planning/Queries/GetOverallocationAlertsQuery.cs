using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record GetOverallocationAlertsQuery() : IRequest<Result<List<OverallocationAlertDto>>>;

internal sealed class GetOverallocationAlertsQueryHandler(IPlanningQueryService service)
    : IRequestHandler<GetOverallocationAlertsQuery, Result<List<OverallocationAlertDto>>>
{
    public async Task<Result<List<OverallocationAlertDto>>> Handle(GetOverallocationAlertsQuery request, CancellationToken cancellationToken)
    {
        var alerts = await service.GetOverallocationAlertsAsync(cancellationToken);
        return Result<List<OverallocationAlertDto>>.Success(alerts);
    }
}
