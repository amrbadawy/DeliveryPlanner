using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class LookupService : ServiceBase, ILookupOrchestrator
{
    public LookupService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<LookupOptionDto>> GetLookupOptionsAsync(string catalog, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        return catalog switch
        {
            LookupCatalogs.TaskStatuses => await GetTaskStatusesAsync(db, includeInactive, cancellationToken),
            LookupCatalogs.DeliveryRisks => await GetDeliveryRisksAsync(db, includeInactive, cancellationToken),
            LookupCatalogs.HolidayTypes => await GetHolidayTypesAsync(db, includeInactive, cancellationToken),
            LookupCatalogs.AdjustmentTypes => await GetAdjustmentTypesAsync(db, includeInactive, cancellationToken),
            LookupCatalogs.ActiveStatuses => await GetActiveStatusesAsync(db, includeInactive, cancellationToken),
            LookupCatalogs.WorkingWeeks => await GetWorkingWeeksAsync(db, includeInactive, cancellationToken),
            _ => []
        };
    }

    public async Task<bool> IsActiveLookupValueAsync(string catalog, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        var normalizedCode = code.Trim().ToUpperInvariant();

        return catalog switch
        {
            LookupCatalogs.TaskStatuses => await db.TaskStatuses.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            LookupCatalogs.DeliveryRisks => await db.DeliveryRisks.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            LookupCatalogs.HolidayTypes => await db.HolidayTypes.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            LookupCatalogs.AdjustmentTypes => await db.AdjustmentTypes.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            LookupCatalogs.ActiveStatuses => await db.ActiveStatuses.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            LookupCatalogs.WorkingWeeks => await db.WorkingWeeks.AnyAsync(x => x.Code == normalizedCode && x.IsActive, cancellationToken),
            _ => false
        };
    }

    private static async Task<List<LookupOptionDto>> GetTaskStatusesAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.TaskStatuses.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<LookupOptionDto>> GetDeliveryRisksAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.DeliveryRisks.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<LookupOptionDto>> GetHolidayTypesAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.HolidayTypes.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<LookupOptionDto>> GetAdjustmentTypesAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.AdjustmentTypes.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<LookupOptionDto>> GetActiveStatusesAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.ActiveStatuses.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<LookupOptionDto>> GetWorkingWeeksAsync(ReadOnlyPlannerDbContext db, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.WorkingWeeks.AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LookupOptionDto(x.Code, x.DisplayName, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
    }
}
