using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class AuditService : IAuditService
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public AuditService(IDbContextFactory<PlannerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task LogAsync(string action, string entityType, string entityId, string description, string? oldValue = null, string? newValue = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entry = AuditEntry.Create(action, entityType, entityId, description, oldValue, newValue);
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditEntry>> GetRecentAsync(int count = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AuditEntries
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
