using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Services;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

public sealed class SchedulingOrchestrator : ISchedulingOrchestrator
{
    private readonly IDbContextFactory<PlannerDbContext> _dbFactory;

    public SchedulingOrchestrator(IDbContextFactory<PlannerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db);
        return scheduler.RunScheduler();
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var scheduler = new SchedulingEngine(db);
        var kpis = scheduler.GetDashboardKPIs();

        var rawFinish = kpis["overall_finish"] as DateTime?;
        var overallFinish = rawFinish.HasValue && rawFinish.Value > DateTime.MinValue ? rawFinish : null;

        return new DashboardKpisDto(
            TotalServices: (int)kpis["total_services"],
            TotalEstimation: (double)kpis["total_estimation"],
            ActiveResources: (int)kpis["active_resources"],
            TotalCapacity: (double)kpis["total_capacity"],
            OverallFinish: overallFinish,
            OnTrack: (int)kpis["on_track"],
            AtRisk: (int)kpis["at_risk"],
            Late: (int)kpis["late"],
            AvgAssigned: (double)kpis["avg_assigned"]);
    }
}
