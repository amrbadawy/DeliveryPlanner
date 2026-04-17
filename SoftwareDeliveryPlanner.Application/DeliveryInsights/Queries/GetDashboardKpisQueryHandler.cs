using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.DeliveryInsights.Queries;

internal sealed class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, Result<DashboardKpisDto>>
{
    private readonly ISchedulerService _orchestrator;

    public GetDashboardKpisQueryHandler(ISchedulerService orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<DashboardKpisDto>> Handle(GetDashboardKpisQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetDashboardKpisAsync(cancellationToken);
}
