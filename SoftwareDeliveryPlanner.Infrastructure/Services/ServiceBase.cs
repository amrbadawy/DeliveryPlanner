using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

/// <summary>
/// Shared infrastructure for all domain services. Provides database context
/// factories, domain event dispatch, and the "save-dispatch-reschedule" pattern
/// used by every mutation method.
/// </summary>
internal abstract class ServiceBase
{
    protected readonly IDbContextFactory<PlannerDbContext> DbFactory;
    protected readonly IDbContextFactory<ReadOnlyPlannerDbContext> ReadOnlyDbFactory;
    protected readonly ISchedulingEngineFactory EngineFactory;
    protected readonly IPublisher Publisher;

    protected ServiceBase(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
    {
        DbFactory = dbFactory;
        ReadOnlyDbFactory = readOnlyDbFactory;
        EngineFactory = engineFactory;
        Publisher = publisher;
    }

    /// <summary>
    /// Saves changes, dispatches domain events, then creates a fresh context
    /// to run the full scheduler. Every mutation method should call this
    /// instead of duplicating the pattern.
    /// </summary>
    protected async Task SaveDispatchAndRescheduleAsync(
        PlannerDbContext db, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(Publisher, cancellationToken);

        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        await engine.RunSchedulerAsync(cancellationToken);
    }

    protected async Task SaveAndDispatchAsync(
        PlannerDbContext db, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await db.DispatchDomainEventsAsync(Publisher, cancellationToken);
    }
}
