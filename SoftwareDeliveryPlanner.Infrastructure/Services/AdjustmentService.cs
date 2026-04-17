using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class AdjustmentService : ServiceBase, IAdjustmentOrchestrator
{
    public AdjustmentService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<Adjustment>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Adjustments.ToListAsync(cancellationToken);
    }

    public async Task AddAdjustmentAsync(
        string resourceId, string adjType, double availabilityPct,
        DateTime adjStart, DateTime adjEnd, string? notes,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        var teamMember = await db.Resources
            .Include(r => r.Adjustments)
            .FirstOrDefaultAsync(r => r.ResourceId == resourceId, cancellationToken)
            ?? throw new DomainException($"Resource '{resourceId}' not found.");

        teamMember.AddAdjustment(adjType, availabilityPct, adjStart, adjEnd, notes);

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var adjustment = await db.Adjustments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (adjustment != null)
        {
            var teamMember = await db.Resources
                .Include(r => r.Adjustments)
                .FirstOrDefaultAsync(r => r.ResourceId == adjustment.ResourceId, cancellationToken);

            teamMember?.RemoveAdjustment(id);
            db.Adjustments.Remove(adjustment);
        }

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }
}
