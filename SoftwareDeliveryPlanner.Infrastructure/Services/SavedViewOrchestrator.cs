using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class SavedViewOrchestrator : ServiceBase, ISavedViewOrchestrator
{
    public SavedViewOrchestrator(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<SavedView>> ListAsync(string pageKey, string? ownerKey, CancellationToken cancellationToken = default)
    {
        var key = pageKey.Trim().ToLowerInvariant();
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedViews
            .Where(v => v.PageKey == key && v.OwnerKey == ownerKey)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedView?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedViews.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<SavedView> UpsertAsync(string name, string pageKey, string payloadJson, string? ownerKey, CancellationToken cancellationToken = default)
    {
        var key = pageKey.Trim().ToLowerInvariant();
        var trimmedName = name.Trim();
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SavedViews
            .FirstOrDefaultAsync(v => v.PageKey == key && v.OwnerKey == ownerKey && v.Name == trimmedName, cancellationToken);

        if (existing != null)
        {
            existing.UpdatePayload(payloadJson);
            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var view = SavedView.Create(trimmedName, key, payloadJson, ownerKey);
        db.SavedViews.Add(view);
        await db.SaveChangesAsync(cancellationToken);
        return view;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var view = await db.SavedViews.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (view != null)
        {
            db.SavedViews.Remove(view);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
