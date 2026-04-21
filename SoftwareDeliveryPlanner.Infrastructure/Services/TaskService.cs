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
        return await db.Tasks
            .Include(t => t.EffortBreakdown)
            .OrderBy(t => t.SchedulingRank ?? 999)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetTaskByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks
            .Include(t => t.EffortBreakdown)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
    }

    public async Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.CountAsync(cancellationToken);
    }

    public async Task UpsertTaskAsync(
        int id, string taskId, string serviceName, double maxResource, int priority,
        List<(string Role, double EstimationDays, double OverlapPct)> effortBreakdown,
        DateTime? strictDate, string? dependsOnTaskIds, bool isNew,
        DateTime? overrideStart = null, string? phase = null, string? preferredResourceIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        if (isNew)
        {
            var task = TaskItem.Create(taskId, serviceName, maxResource, priority, effortBreakdown,
                strictDate, dependsOnTaskIds, overrideStart, phase, preferredResourceIds);
            db.Tasks.Add(task);
        }
        else
        {
            var existing = await db.Tasks
                .Include(t => t.EffortBreakdown)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            existing?.Update(serviceName, maxResource, priority, effortBreakdown,
                strictDate, dependsOnTaskIds, phase, preferredResourceIds);
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
