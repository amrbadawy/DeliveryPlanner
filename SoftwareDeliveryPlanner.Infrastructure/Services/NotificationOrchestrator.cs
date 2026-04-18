using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class NotificationOrchestrator : ServiceBase, INotificationOrchestrator
{
    public NotificationOrchestrator(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<RiskNotification>> GetNotificationsAsync(bool unreadOnly)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync();
        var query = db.RiskNotifications.AsQueryable();

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var unread = await db.RiskNotifications
            .Where(n => !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.MarkAsRead();

        await db.SaveChangesAsync();
    }
}
