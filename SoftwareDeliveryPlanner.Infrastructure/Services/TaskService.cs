using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class TaskService : ServiceBase, ITaskOrchestrator
{
    public TaskService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<TaskItem>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.OrderBy(t => t.SchedulingRank ?? 999).ToListAsync(cancellationToken);
    }

    public async Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.CountAsync(cancellationToken);
    }

    public async Task UpsertTaskAsync(
        int id, string taskId, string serviceName, double devEstimation,
        double maxDev, int priority, DateTime? strictDate,
        string? dependsOnTaskIds, bool isNew,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var task = TaskItem.Create(taskId, serviceName, devEstimation, maxDev, priority, strictDate, dependsOnTaskIds);
            db.Tasks.Add(task);
        }
        else
        {
            var existing = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            existing?.Update(serviceName, devEstimation, maxDev, priority, strictDate, dependsOnTaskIds);
        }

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }

    public async Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task != null)
            db.Tasks.Remove(task);

        await SaveDispatchAndRescheduleAsync(db, cancellationToken);
    }
}
