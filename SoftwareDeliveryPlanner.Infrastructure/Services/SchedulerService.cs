using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
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
        var result = engine.RunScheduler();

        // Record last scheduler run timestamp
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var now = TimeProvider.System.GetUtcNow().DateTime;

        var setting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.LastSchedulerRun, cancellationToken);
        if (setting is not null)
            setting.Value = now.ToString("O");
        else
            db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.LastSchedulerRun, Value = now.ToString("O") });

        // Record scheduler snapshot for risk trend
        var kpis = engine.GetDashboardKPIs();
        var snapshot = SchedulerSnapshot.Create(
            now,
            onTrack: (int)kpis["on_track"],
            atRisk: (int)kpis["at_risk"],
            late: (int)kpis["late"],
            total: (int)kpis["total_services"]);
        db.SchedulerSnapshots.Add(snapshot);

        await db.SaveChangesAsync(cancellationToken);

        return result;
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
