using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Notifications.Queries;

internal sealed class GetRiskNotificationsQueryHandler : IRequestHandler<GetRiskNotificationsQuery, Result<List<RiskNotification>>>
{
    private readonly INotificationOrchestrator _orchestrator;

    public GetRiskNotificationsQueryHandler(INotificationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result<List<RiskNotification>>> Handle(GetRiskNotificationsQuery request, CancellationToken cancellationToken)
        => await _orchestrator.GetNotificationsAsync(request.UnreadOnly);
}
