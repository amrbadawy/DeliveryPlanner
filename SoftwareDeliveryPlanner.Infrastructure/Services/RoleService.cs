using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class RoleService : ServiceBase, IRoleOrchestrator
{
    public RoleService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<Role>> GetRolesAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.Roles.AsQueryable();
        if (!includeInactive)
            query = query.Where(r => r.IsActive);

        return await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertRoleAsync(
        int id,
        string code,
        string displayName,
        bool isActive,
        int sortOrder,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedCode = code.Trim();
        var normalizedName = displayName.Trim();

        if (isNew)
        {
            db.Roles.Add(new Role
            {
                Code = normalizedCode,
                DisplayName = normalizedName,
                IsActive = isActive,
                SortOrder = sortOrder
            });
        }
        else
        {
            var existing = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (existing is not null)
            {
                var oldCode = existing.Code;
                var codeChanged = !string.Equals(oldCode, normalizedCode, StringComparison.Ordinal);

                existing.Code = normalizedCode;
                existing.DisplayName = normalizedName;
                existing.IsActive = isActive;
                existing.SortOrder = sortOrder;

                if (codeChanged)
                {
                    var members = await db.Resources.Where(r => r.Role == oldCode).ToListAsync(cancellationToken);
                    foreach (var member in members)
                        member.Update(member.ResourceName, normalizedCode, member.Team, member.AvailabilityPct, member.DailyCapacity, member.StartDate, member.Active, member.Notes);
                }
            }
        }

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role != null)
            db.Roles.Remove(role);

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task<bool> RoleCodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = db.Roles.Where(r => r.Code.ToUpper() == normalizedCode);

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsRoleInUseAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Resources.AnyAsync(r => r.Role == code, cancellationToken);
    }
}
