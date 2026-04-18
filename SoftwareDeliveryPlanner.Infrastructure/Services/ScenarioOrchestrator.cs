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

    public async Task SaveScenarioAsync(PlanScenario scenario)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.PlanScenarios.Add(scenario);
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
