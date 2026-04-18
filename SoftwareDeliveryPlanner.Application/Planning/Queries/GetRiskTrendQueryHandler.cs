using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

internal sealed class GetRiskTrendQueryHandler : IRequestHandler<GetRiskTrendQuery, Result<List<RiskTrendPointDto>>>
{
    private readonly IPlanningQueryService _service;

    public GetRiskTrendQueryHandler(IPlanningQueryService service)
        => _service = service;

    public async Task<Result<List<RiskTrendPointDto>>> Handle(GetRiskTrendQuery request, CancellationToken cancellationToken)
        => await _service.GetRiskTrendAsync(request.MaxPoints);
}
