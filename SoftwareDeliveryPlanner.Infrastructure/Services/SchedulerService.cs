using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;

namespace SoftwareDeliveryPlanner.Infrastructure.Services;

internal sealed class SchedulerService : ServiceBase, ISchedulerService
{
    // Meter for scheduling metrics — registered in Program.cs via AddMeter().
    // Metrics are exported to the Aspire dashboard Metrics tab and any OTLP-compatible APM.
    private static readonly Meter Meter = new("SoftwareDeliveryPlanner.Scheduling", "1.0.0");
    private static readonly Histogram<double> RunDuration =
        Meter.CreateHistogram<double>("scheduling.run.duration", unit: "ms",
            description: "Duration of each scheduler run in milliseconds");
    private static readonly Histogram<int> TasksScheduled =
        Meter.CreateHistogram<int>("scheduling.tasks.scheduled",
            description: "Number of tasks processed per scheduler run");
    private static readonly Histogram<int> AtRiskCount =
        Meter.CreateHistogram<int>("scheduling.risk.atrisk_count",
            description: "Number of AtRisk + Late tasks after each scheduler run");

    public SchedulerService(
        IDbContextFactory<PlannerDbContext> dbFactory,
        IDbContextFactory<ReadOnlyPlannerDbContext> readOnlyDbFactory,
        ISchedulingEngineFactory engineFactory,
        IPublisher publisher)
        : base(dbFactory, readOnlyDbFactory, engineFactory, publisher) { }

    public async Task<string> RunSchedulerAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Capture pre-run risk states for notification detection
        await using var db = await DbFactory.CreateDbContextAsync(cancellationToken);
        var preRunRisks = await db.Tasks
            .Where(t => t.PlannedStart != null)
            .Select(t => new { t.TaskId, t.ServiceName, t.DeliveryRisk })
            .ToListAsync(cancellationToken);

        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        var result = engine.RunScheduler();

        // Record last scheduler run timestamp
        var now = TimeProvider.System.GetUtcNow().DateTime;
        var setting = await db.Settings
            .FirstOrDefaultAsync(s => s.Key == DomainConstants.SettingKeys.LastSchedulerRun, cancellationToken);
        if (setting is not null)
            setting.Value = now.ToString("O");
        else
            db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.LastSchedulerRun, Value = now.ToString("O") });

        // Record scheduler snapshot for risk trend
        var kpis = engine.GetDashboardKPIs();
        var onTrack = (int)kpis["on_track"];
        var atRisk = (int)kpis["at_risk"];
        var late = (int)kpis["late"];
        var total = (int)kpis["total_services"];

        var snapshot = SchedulerSnapshot.Create(now,
            onTrack: onTrack, atRisk: atRisk, late: late, total: total);
        db.SchedulerSnapshots.Add(snapshot);

        // Detect risk changes and create notifications
        var postRunTasks = await db.Tasks
            .Where(t => t.PlannedStart != null)
            .Select(t => new { t.TaskId, t.ServiceName, t.DeliveryRisk })
            .ToListAsync(cancellationToken);

        foreach (var post in postRunTasks)
        {
            var pre = preRunRisks.FirstOrDefault(p => p.TaskId == post.TaskId);
            if (pre is null) continue;

            // Only notify when risk worsens (OnTrack→AtRisk, OnTrack→Late, AtRisk→Late)
            var worsened = (pre.DeliveryRisk, post.DeliveryRisk) switch
            {
                (DomainConstants.DeliveryRisk.OnTrack, DomainConstants.DeliveryRisk.AtRisk) => true,
                (DomainConstants.DeliveryRisk.OnTrack, DomainConstants.DeliveryRisk.Late) => true,
                (DomainConstants.DeliveryRisk.AtRisk, DomainConstants.DeliveryRisk.Late) => true,
                _ => false
            };

            if (worsened)
            {
                db.RiskNotifications.Add(
                    RiskNotification.Create(post.TaskId, post.ServiceName, pre.DeliveryRisk, post.DeliveryRisk));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Record scheduling metrics for Aspire dashboard / APM
        sw.Stop();
        RunDuration.Record(sw.Elapsed.TotalMilliseconds);
        TasksScheduled.Record(total);
        AtRiskCount.Record(atRisk + late);

        return result;
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        var kpis = engine.GetDashboardKPIs();

        var rawFinish = kpis["overall_finish"] as DateTime?;
        var overallFinish = rawFinish.HasValue && rawFinish.Value > DateTime.MinValue ? rawFinish : null;

        var rawStart = kpis["earliest_start"] as DateTime?;
        var earliestStart = rawStart.HasValue && rawStart.Value > DateTime.MinValue ? rawStart : null;

        var dto = new DashboardKpisDto(
            TotalServices: (int)kpis["total_services"],
            TotalEstimation: (double)kpis["total_estimation"],
            ActiveResources: (int)kpis["active_resources"],
            TotalCapacity: (double)kpis["total_capacity"],
            EarliestStart: earliestStart,
            OverallFinish: overallFinish,
            OnTrack: (int)kpis["on_track"],
            AtRisk: (int)kpis["at_risk"],
            Late: (int)kpis["late"],
            Unscheduled: (int)kpis["unscheduled"],
            AvgAssigned: (double)kpis["avg_assigned"],
            OverallocationCount: (int)kpis["overallocation_count"]);

        Debug.Assert(
            dto.OnTrack + dto.AtRisk + dto.Late + dto.Unscheduled == dto.TotalServices,
            $"KPI sum invariant violated: {dto.OnTrack} + {dto.AtRisk} + {dto.Late} + {dto.Unscheduled} != {dto.TotalServices}");

        return dto;
    }

    public async Task<ScheduleDiffDto> PreviewScheduleAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        return engine.PreviewSchedule();
    }

    public async Task FreezeBaselineAsync(CancellationToken cancellationToken = default)
    {
        using var engine = await EngineFactory.CreateAsync(cancellationToken);
        engine.FreezeBaseline();
    }
}
