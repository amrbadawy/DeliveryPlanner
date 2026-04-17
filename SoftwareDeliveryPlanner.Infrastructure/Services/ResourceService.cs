using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class ResourceService : ServiceBase, IResourceOrchestrator
{
    public ResourceService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<TeamMember>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.ToListAsync(cancellationToken);
    }

    public async Task<int> GetResourceCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.CountAsync(cancellationToken);
    }

    public async Task UpsertResourceAsync(
        int id, string resourceId, string resourceName, string role,
        string team, double availabilityPct, double dailyCapacity,
        DateTime startDate, string active, string? notes, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var resource = TeamMember.Create(resourceId, resourceName, role, team, availabilityPct, dailyCapacity, startDate, active: active, notes: notes);
            db.Resources.Add(resource);
        }
        else
        {
            var existing = await db.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            existing?.Update(resourceName, role, team, availabilityPct, dailyCapacity, startDate, active, notes);
        }

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task DeleteResourceAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (resource != null)
            db.Resources.Remove(resource);

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }
}
