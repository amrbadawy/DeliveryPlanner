using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Notifications.Commands;

internal sealed class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand, Result>
{
    private readonly INotificationOrchestrator _orchestrator;

    public MarkNotificationsReadCommandHandler(INotificationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public async Task<Result> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await _orchestrator.MarkAllAsReadAsync();
        return Result.Success();
    }
}
