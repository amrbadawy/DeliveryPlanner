using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record GetUtilizationForecastQuery(int WeeksAhead = 26) : IRequest<Result<UtilizationForecastDto>>;

internal sealed class GetUtilizationForecastQueryHandler(IPlanningQueryService service)
    : IRequestHandler<GetUtilizationForecastQuery, Result<UtilizationForecastDto>>
{
    public async Task<Result<UtilizationForecastDto>> Handle(GetUtilizationForecastQuery request, CancellationToken cancellationToken)
    {
        var forecast = await service.GetUtilizationForecastAsync(request.WeeksAhead, cancellationToken);
        return Result<UtilizationForecastDto>.Success(forecast);
    }
}
