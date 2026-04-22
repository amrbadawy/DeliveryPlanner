using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class DomainEventAuditHandler : INotificationHandler<DomainEventNotification>
{
    private readonly IAuditService _audit;

    public DomainEventAuditHandler(IAuditService audit) => _audit = audit;

    private readonly record struct AuditContext(
        string? Action,
        string? EntityType,
        string? EntityId,
        string? Description);

    public async Task Handle(DomainEventNotification notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var ctx = domainEvent switch
        {
            TaskCreatedEvent e => new AuditContext(DomainConstants.AuditAction.Created, DomainConstants.EntityType.Task,
                e.TaskId, $"Task '{e.ServiceName}' created"),

            TaskUpdatedEvent e => new AuditContext(DomainConstants.AuditAction.Updated, DomainConstants.EntityType.Task,
                e.TaskId, $"Task '{e.TaskId}' updated"),

            ResourceCreatedEvent e => new AuditContext(DomainConstants.AuditAction.Created, DomainConstants.EntityType.Resource,
                e.ResourceId, $"Resource '{e.ResourceName}' created"),

            ResourceUpdatedEvent e => new AuditContext(DomainConstants.AuditAction.Updated, DomainConstants.EntityType.Resource,
                e.ResourceId, $"Resource '{e.ResourceId}' updated"),

            HolidayCreatedEvent e => new AuditContext(DomainConstants.AuditAction.Created, DomainConstants.EntityType.Holiday,
                string.Empty, $"Holiday '{e.HolidayName}' created starting {e.StartDate:yyyy-MM-dd}"),

            HolidayUpdatedEvent e => new AuditContext(DomainConstants.AuditAction.Updated, DomainConstants.EntityType.Holiday,
                e.HolidayId.ToString(), $"Holiday #{e.HolidayId} updated"),

            AdjustmentAddedEvent e => new AuditContext(DomainConstants.AuditAction.Created, DomainConstants.EntityType.Adjustment,
                e.ResourceId, $"Adjustment '{e.AdjType}' added for resource '{e.ResourceId}'"),

            AdjustmentRemovedEvent e => new AuditContext(DomainConstants.AuditAction.Deleted, DomainConstants.EntityType.Adjustment,
                e.AdjustmentId.ToString(), $"Adjustment #{e.AdjustmentId} removed from resource '{e.ResourceId}'"),

            _ => default
        };

        if (ctx.Action is not null)
        {
            await _audit.LogAsync(ctx.Action, ctx.EntityType!, ctx.EntityId!, ctx.Description!);
        }
    }
}
