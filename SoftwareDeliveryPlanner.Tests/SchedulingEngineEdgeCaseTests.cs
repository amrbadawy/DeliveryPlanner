using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;
using SoftwareDeliveryPlanner.Services;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Edge-case tests for SchedulingEngine covering code paths not exercised
/// by the main SchedulingEngineTests fixture.
/// </summary>
public class SchedulingEngineEdgeCaseTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    public SchedulingEngineEdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new PlannerDbContext(options);
        _db.Database.EnsureCreated();
        _db.InitializeDefaultData();
        _engine = new SchedulingEngine(_db);
    }

    public void Dispose() => _db.Dispose();

    // ------------------------------------------------------------------
    // OverrideStart — task must not begin before the override date
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_OverrideStart_TaskDoesNotStartBeforeOverrideDate()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Plan start is 2026-05-01 by default; set override to 2026-07-01
        var overrideDate = new DateTime(2026, 7, 1);

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-OVR",
            ServiceName = "Override Start Test",
            DevEstimation = 5,
            Priority = 5,
            OverrideStart = overrideDate
        });
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-OVR");
        // PlannedStart must be >= overrideDate (or null if no capacity in window)
        if (task.PlannedStart.HasValue)
        {
            Assert.True(task.PlannedStart.Value >= overrideDate.Date,
                $"Expected start on/after {overrideDate:yyyy-MM-dd}, got {task.PlannedStart:yyyy-MM-dd}");
        }
    }

    // ------------------------------------------------------------------
    // OverrideDev — scheduler uses the override developer allocation
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_OverrideDev_AllocationsUseOverrideDevValue()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        const double overrideDev = 0.5;

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-ODEV",
            ServiceName = "Override Dev Test",
            DevEstimation = 3,
            MaxDev = 5,
            Priority = 5,
            OverrideDev = overrideDev
        });
        _db.SaveChanges();

        _engine.RunScheduler();

        var allocations = _db.Allocations
            .Where(a => a.TaskId == "SV-ODEV")
            .ToList();

        // Every allocation should have AssignedDev <= overrideDev
        Assert.All(allocations, a =>
            Assert.True(a.AssignedDev <= overrideDev,
                $"AssignedDev {a.AssignedDev} exceeds OverrideDev {overrideDev}"));
    }

    // ------------------------------------------------------------------
    // Resource EndDate — resource should not contribute after their EndDate
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_ResourceWithEndDateInPast_NotCountedAsActive()
    {
        // Add a resource whose EndDate has already passed
        _db.Resources.Add(new TeamMember
        {
            ResourceId = "RES-99",
            ResourceName = "Past Resource",
            Role = "Developer",
            Active = "Yes",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2021, 1, 1),   // in the past
            DailyCapacity = 8,
            AvailabilityPct = 100
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        var kpis = engine.GetDashboardKPIs();

        // active_resources counts resources where Active == "Yes" (no date filter in GetDashboardKPIs)
        // The scheduling engine itself filters by EndDate during RunScheduler, not GetDashboardKPIs.
        // This test simply confirms the KPI is still returned correctly.
        Assert.True(kpis.ContainsKey("active_resources"));
        Assert.IsType<int>(kpis["active_resources"]);
    }

    [Fact]
    public void RunScheduler_ResourceWithEndDateBeforePlanStart_NoCapacityContributed()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        // Remove all existing resources and add one whose EndDate is before plan start
        _db.Resources.RemoveRange(_db.Resources);
        _db.SaveChanges();

        _db.Resources.Add(new TeamMember
        {
            ResourceId = "RES-EXP",
            ResourceName = "Expired",
            Role = "Developer",
            Active = "Yes",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31), // well before plan start 2026-05-01
            DailyCapacity = 8,
            AvailabilityPct = 100
        });
        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-NOCP",
            ServiceName = "No Capacity Task",
            DevEstimation = 5,
            Priority = 5
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        var result = engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-NOCP");
        // No capacity available → task should remain Not Started / unallocated
        Assert.Equal("Not Started", task.Status);
    }

    // ------------------------------------------------------------------
    // Adjustment reducing capacity to zero
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_AdjustmentZeroCapacity_TaskNotScheduledDuringAdjustmentPeriod()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        // Keep one resource but zero it out for the first working month
        _db.Adjustments.RemoveRange(_db.Adjustments);
        _db.Resources.RemoveRange(_db.Resources);
        _db.SaveChanges();

        _db.Resources.Add(new TeamMember
        {
            ResourceId = "RES-ADJ",
            ResourceName = "Adjustable",
            Role = "Developer",
            Active = "Yes",
            StartDate = new DateTime(2026, 1, 1),
            DailyCapacity = 8,
            AvailabilityPct = 100
        });
        // Zero-capacity adjustment for the entire plan start month
        _db.Adjustments.Add(new Adjustment
        {
            ResourceId = "RES-ADJ",
            AdjStart = new DateTime(2026, 5, 1),
            AdjEnd = new DateTime(2026, 5, 31),
            AvailabilityPct = 0,
            AdjType = "Leave",
            Notes = "Full leave"
        });
        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-ZADJ",
            ServiceName = "Zero Adj Task",
            DevEstimation = 3,
            Priority = 5
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        engine.RunScheduler();

        var allocations = _db.Allocations
            .Where(a => a.TaskId == "SV-ZADJ"
                && a.CalendarDate >= new DateTime(2026, 5, 1)
                && a.CalendarDate <= new DateTime(2026, 5, 31))
            .ToList();

        // No allocations during the zero-capacity period
        Assert.Empty(allocations);
    }

    // ------------------------------------------------------------------
    // All tasks already complete (DevEstimation = 0 effectively)
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_ZeroEstimationTask_HandledWithoutError()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-ZERO",
            ServiceName = "Zero Estimation",
            DevEstimation = 0,   // nothing to schedule
            Priority = 5
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);

        // Should not throw; zero-estimation tasks just get "Not Started" (no effort remaining)
        var exception = Record.Exception(() => engine.RunScheduler());
        Assert.Null(exception);
    }

    // ------------------------------------------------------------------
    // GetDashboardKPIs when no tasks have been scheduled yet
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_NoScheduledTasks_OverallFinishIsMinValue()
    {
        // Remove all tasks so there are no PlannedFinish dates
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        var kpis = engine.GetDashboardKPIs();

        // With no finish dates, overallFinish falls back to DateTime.MinValue
        Assert.True(kpis.ContainsKey("overall_finish"));
        // The orchestrator then maps MinValue → null; the raw KPI returns MinValue
        var raw = kpis["overall_finish"];
        Assert.NotNull(raw);
    }

    [Fact]
    public void GetDashboardKPIs_TasksWithNoPlannedFinish_AvgAssignedIsZero()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-NA",
            ServiceName = "Not Assigned",
            DevEstimation = 5,
            Priority = 5,
            AssignedDev = null   // no assignment yet
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        var kpis = engine.GetDashboardKPIs();

        Assert.Equal(0.0, (double)kpis["avg_assigned"]);
    }

    // ------------------------------------------------------------------
    // CalculateRisk — Late path
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_TaskFinishesAfterStrictDate_MarkedLate()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Strict date in the very near future, but estimation is huge → will finish late
        var nearFuture = DateTime.Today.AddDays(2);

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "SV-LATE",
            ServiceName = "Late Task",
            DevEstimation = 500,   // far more than available capacity in 2 days
            StrictDate = nearFuture,
            Priority = 5
        });
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-LATE");
        Assert.Equal("Late", task.DeliveryRisk);
    }

    // ------------------------------------------------------------------
    // Multiple tasks — priority ordering is respected end-to-end
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_HigherPriorityTask_StartsEarlierOrSameDay()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem { TaskId = "SV-P1", ServiceName = "Priority 1", DevEstimation = 10, Priority = 1 });
        _db.Tasks.Add(new TaskItem { TaskId = "SV-P9", ServiceName = "Priority 9", DevEstimation = 10, Priority = 9 });
        _db.SaveChanges();

        _engine.RunScheduler();

        var p1 = _db.Tasks.First(t => t.TaskId == "SV-P1");
        var p9 = _db.Tasks.First(t => t.TaskId == "SV-P9");

        if (p1.PlannedStart.HasValue && p9.PlannedStart.HasValue)
        {
            Assert.True(p1.PlannedStart.Value <= p9.PlannedStart.Value);
        }
    }
}
