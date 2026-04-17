using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Infrastructure.Extensions;

/// <summary>
/// Extension methods for dispatching domain events collected by aggregate roots
/// after persistence via <see cref="DbContext.SaveChangesAsync"/>.
/// </summary>
internal static class DbContextExtensions
{
    /// <summary>
    /// Dispatches all pending domain events from tracked aggregate roots, then clears them.
    /// Call after <see cref="DbContext.SaveChangesAsync"/> to ensure events are published post-commit.
    /// </summary>
    public static async Task DispatchDomainEventsAsync(
        this DbContext context, IPublisher publisher, CancellationToken ct = default)
    {
        var aggregateRoots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregateRoots.SelectMany(r => r.DomainEvents).ToList();

        foreach (var root in aggregateRoots)
            root.ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            var notification = new DomainEventNotification(domainEvent);
            await publisher.Publish(notification, ct);
        }
    }
}

/// <summary>
/// Wraps an <see cref="IDomainEvent"/> as a MediatR <see cref="INotification"/> for dispatch.
/// Subscribe to <see cref="INotificationHandler{DomainEventNotification}"/> to handle all events.
/// </summary>
internal sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
