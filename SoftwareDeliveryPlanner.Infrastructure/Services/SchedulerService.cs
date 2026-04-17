using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class SchedulerService : ServiceBase, ISchedulerService
{
    public SchedulerService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        return engine.RunScheduler();
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        var kpis = engine.GetDashboardKPIs();

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
