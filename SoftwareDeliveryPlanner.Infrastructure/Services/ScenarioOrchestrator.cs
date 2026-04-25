using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class ScenarioOrchestrator : ServiceBase, IScenarioOrchestrator
{
    public ScenarioOrchestrator(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<List<PlanScenario>> GetScenariosAsync()
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync();
        return await db.PlanScenarios
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<PlanScenario?> GetScenarioWithSnapshotsAsync(int id)
    {
        await using var db = await ReadOnlyDbFactory.CreateDbContextAsync();
        return await db.PlanScenarios
            .Include(s => s.TaskSnapshots)
                .ThenInclude(ts => ts.EffortSnapshots)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task SaveScenarioAsync(PlanScenario scenario)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.PlanScenarios.Add(scenario);
        await db.SaveChangesAsync();
    }

    public async Task SaveScenarioWithSnapshotsAsync(PlanScenario scenario, List<TaskItem> tasks)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.PlanScenarios.Add(scenario);

        // Must save first to get the scenario's auto-generated Id
        await db.SaveChangesAsync();

        foreach (var task in tasks)
        {
            var effortData = task.EffortBreakdown
                .Select(e => new EffortSnapshotSpec(e.Role, e.EstimationDays, e.OverlapPct, e.SortOrder))
                .ToList();

            var snapshot = ScenarioTaskSnapshot.Create(
                scenario.Id,
                task.TaskId,
                task.ServiceName,
                task.Priority,
                task.SchedulingRank,
                task.PlannedStart,
                task.PlannedFinish,
                task.Duration,
                task.StrictDate,
                task.AssignedResourceId,
                task.PeakConcurrency,
                task.Status,
                task.DeliveryRisk,
                task.DependsOnTaskIds,
                task.Phase,
                effortData);

            db.ScenarioTaskSnapshots.Add(snapshot);
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteScenarioAsync(int id)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var scenario = await db.PlanScenarios.FirstOrDefaultAsync(s => s.Id == id);
        if (scenario != null)
        {
            db.PlanScenarios.Remove(scenario);
            await db.SaveChangesAsync();
        }
    }
}
