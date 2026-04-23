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
            .Include(t => t.Dependencies)
            .OrderBy(t => t.SchedulingRank ?? 999)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetTaskByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks
            .Include(t => t.EffortBreakdown)
            .Include(t => t.Dependencies)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
    }

    public async Task<int> GetTaskCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks.CountAsync(cancellationToken);
    }

    public async Task UpsertTaskAsync(
        int id, string taskId, string serviceName,
        int priority,
        List<(string Role, double EstimationDays, double OverlapPct, double MaxFte)> effortBreakdown,
        DateTime? strictDate,
        List<(string PredecessorTaskId, string Type, int LagDays, double OverlapPct)>? dependencies,
        bool isNew,
        DateTime? overrideStart = null, string? phase = null, string? preferredResourceIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);

        var specs = effortBreakdown
            .Select(e => new EffortBreakdownSpec(e.Role, e.EstimationDays, e.OverlapPct, e.MaxFte))
            .ToList();

        TaskItem task;
        if (isNew)
        {
            task = TaskItem.Create(taskId, serviceName, priority, specs,
                strictDate, overrideStart, phase, preferredResourceIds);

            // Add dependencies to the domain object BEFORE adding to EF so the
            // graph traversal at Add() time picks them up.
            if (dependencies != null)
                foreach (var (predId, type, lagDays, overlapPct) in dependencies)
                    task.AddDependency(predId, type, lagDays, overlapPct);

            db.Tasks.Add(task);
        }
        else
        {
            task = (await db.Tasks
                .Include(t => t.EffortBreakdown)
                .Include(t => t.Dependencies)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken))!;
            task.Update(serviceName, priority, specs,
                strictDate, phase, preferredResourceIds);

            // For updates, EF can't detect Clear() on a private backing field,
            // so explicitly tell EF to delete the old rows, then add new ones.
            var existingDeps = task.Dependencies.ToList();
            db.Set<TaskDependency>().RemoveRange(existingDeps);
            task.ClearDependencies();

            if (dependencies != null)
            {
                foreach (var (predId, type, lagDays, overlapPct) in dependencies)
                {
                    var newDep = task.AddDependency(predId, type, lagDays, overlapPct);
                    db.Set<TaskDependency>().Add(newDep);
                }
            }
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
