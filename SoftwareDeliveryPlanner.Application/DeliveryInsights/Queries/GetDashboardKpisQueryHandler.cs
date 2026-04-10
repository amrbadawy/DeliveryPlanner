using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;

namespace SoftwareDeliveryPlanner.Application.DeliveryInsights.Queries;

public sealed class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardKpisDto>
{
    private readonly ISchedulingOrchestrator _orchestrator;

    public GetDashboardKpisQueryHandler(ISchedulingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<DashboardKpisDto> Handle(GetDashboardKpisQuery request, CancellationToken cancellationToken)
    {
        return await _orchestrator.GetDashboardKpisAsync(cancellationToken);
    }
}
