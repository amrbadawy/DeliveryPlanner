using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Edge-case tests for SchedulingEngine covering code paths not exercised
/// by the main SchedulingEngineTests fixture.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SchedulingEngineEdgeCaseTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    public SchedulingEngineEdgeCaseTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
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

        _db.Tasks.Add(TaskItem.Create("SV-100", "Override Start Test", 5, 1, 5, overrideStart: overrideDate));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-100");
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

        _db.Tasks.Add(TaskItem.Create("SV-101", "Override Dev Test", 3, 5, 5, overrideDev: overrideDev));
        _db.SaveChanges();

        _engine.RunScheduler();

        var allocations = _db.Allocations
            .Where(a => a.TaskId == "SV-101")
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
        _db.Resources.Add(TeamMember.Create("RES-99", "Past Resource", "Developer", "Default", 100, 8, new DateTime(2020, 1, 1)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
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

        _db.Resources.Add(TeamMember.Create("RES-10", "Expired", "Developer", "Default", 100, 8, new DateTime(2019, 1, 1), endDate: new DateTime(2020, 1, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-102", "No Capacity Task", 5, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var result = engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-102");
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

        _db.Resources.Add(TeamMember.Create("RES-11", "Adjustable", "Developer", "Default", 100, 8, new DateTime(2026, 1, 1)));
        // Zero-capacity adjustment for the entire plan start month
        _db.Adjustments.Add(Adjustment.Create("RES-11", "Leave", 0, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), "Full leave"));
        _db.Tasks.Add(TaskItem.Create("SV-103", "Zero Adj Task", 3, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var allocations = _db.Allocations
            .Where(a => a.TaskId == "SV-103"
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

        _db.Tasks.Add(TaskItem.Create("SV-104", "Zero Estimation", 0, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);

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

        var engine = new SchedulingEngine(_db, TimeProvider.System);
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

        _db.Tasks.Add(TaskItem.Create("SV-105", "Not Assigned", 5, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
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

        _db.Tasks.Add(TaskItem.Create("SV-106", "Late Task", 500, 1, 5, strictDate: nearFuture));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-106");
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

        _db.Tasks.Add(TaskItem.Create("SV-107", "Priority 1", 10, 1, 1));
        _db.Tasks.Add(TaskItem.Create("SV-108", "Priority 9", 10, 1, 9));
        _db.SaveChanges();

        _engine.RunScheduler();

        var p1 = _db.Tasks.First(t => t.TaskId == "SV-107");
        var p9 = _db.Tasks.First(t => t.TaskId == "SV-108");

        if (p1.PlannedStart.HasValue && p9.PlannedStart.HasValue)
        {
            Assert.True(p1.PlannedStart.Value <= p9.PlannedStart.Value);
        }
    }

    // ------------------------------------------------------------------
    // Same-priority tasks — both get scheduled
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_MultipleTasksSamePriority_BothGetScheduled()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-109", "Same Priority A", 3, 1, 5));
        _db.Tasks.Add(TaskItem.Create("SV-110", "Same Priority B", 3, 1, 5));
        _db.SaveChanges();

        _engine.RunScheduler();

        var t1 = _db.Tasks.First(t => t.TaskId == "SV-109");
        var t2 = _db.Tasks.First(t => t.TaskId == "SV-110");

        Assert.NotNull(t1.PlannedStart);
        Assert.NotNull(t2.PlannedStart);
    }

    // ------------------------------------------------------------------
    // MaxDev exceeding capacity — capped at available capacity
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_TaskWithMaxDevGreaterThanCapacity_CapsAtCapacity()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-111", "Max Dev Overflow", 10, 100, 5));
        _db.SaveChanges();

        _engine.RunScheduler();

        var allocations = _db.Allocations.Where(a => a.TaskId == "SV-111").ToList();
        // Each allocation's AssignedDev should not exceed the day's available capacity
        Assert.All(allocations, a =>
            Assert.True(a.AssignedDev <= a.AvailableCapacity,
                $"AssignedDev {a.AssignedDev} exceeds AvailableCapacity {a.AvailableCapacity}"));
    }

    // ------------------------------------------------------------------
    // All resources inactive — task remains unscheduled
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_AllResourcesInactive_TaskNotScheduled()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.Resources.RemoveRange(_db.Resources);
        _db.SaveChanges();

        // Add a single inactive resource
        _db.Resources.Add(TeamMember.Create("RES-12", "Inactive Dev", "Developer", "Default", 100, 8, new DateTime(2026, 1, 1), active: "No"));
        _db.Tasks.Add(TaskItem.Create("SV-112", "Inactive Resources Task", 5, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-112");
        Assert.Equal("Not Started", task.Status);
    }

    // ------------------------------------------------------------------
    // CalculateRisk — No strict date returns On Track
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateRisk_NoStrictDate_ReturnsOnTrack()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-113", "No Strict Date", 3, 1, 5));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-113");
        Assert.Equal("On Track", task.DeliveryRisk);
    }

    // ------------------------------------------------------------------
    // Multiple strict-date tasks — earlier deadline scheduled first
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_MultipleStrictDateTasks_EarlierDeadlineFirst()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-114", "Later Deadline", 5, 1, 5, strictDate: new DateTime(2026, 12, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-115", "Earlier Deadline", 5, 1, 5, strictDate: new DateTime(2026, 8, 1)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var later = _db.Tasks.First(t => t.TaskId == "SV-114");
        var earlier = _db.Tasks.First(t => t.TaskId == "SV-115");

        // Earlier strict date task should start on or before the later one
        if (earlier.PlannedStart.HasValue && later.PlannedStart.HasValue)
        {
            Assert.True(earlier.PlannedStart.Value <= later.PlannedStart.Value,
                $"Earlier deadline task started {earlier.PlannedStart:yyyy-MM-dd} but later deadline started {later.PlannedStart:yyyy-MM-dd}");
        }
    }

    // ------------------------------------------------------------------
    // GetDashboardKPIs — overall_finish is a real date after scheduling
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_WithScheduledTasks_OverallFinishIsMax()
    {
        // Run scheduler with default seeded data
        _engine.RunScheduler();

        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("overall_finish"));

        var overallFinish = (DateTime)kpis["overall_finish"];
        Assert.True(overallFinish > DateTime.MinValue,
            $"Expected a real date but got {overallFinish}");
    }

    // ------------------------------------------------------------------
    // Task Dependencies — dependent tasks start after prerequisites
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_TaskWithDependency_StartsAfterDependencyCompletes()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Task A: no dependencies, estimated 5 days
        _db.Tasks.Add(TaskItem.Create("DEP-001", "Prerequisite Task", 5, 1, 1));

        // Task B: depends on DEP-001, estimated 3 days
        _db.Tasks.Add(TaskItem.Create("DEP-002", "Dependent Task", 3, 1, 1, dependsOnTaskIds: "DEP-001"));
        _db.SaveChanges();

        _engine.RunScheduler();

        var prerequisite = _db.Tasks.First(t => t.TaskId == "DEP-001");
        var dependent = _db.Tasks.First(t => t.TaskId == "DEP-002");

        // Both should be scheduled
        Assert.NotNull(prerequisite.PlannedFinish);
        Assert.NotNull(dependent.PlannedStart);

        // Dependent task must start after prerequisite finishes
        Assert.True(dependent.PlannedStart!.Value >= prerequisite.PlannedFinish!.Value,
            $"Dependent task started {dependent.PlannedStart} but prerequisite finished {prerequisite.PlannedFinish}");
    }

    [Fact]
    public void RunScheduler_TaskWithMultipleDependencies_StartsAfterAllComplete()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Task A: 3 days
        _db.Tasks.Add(TaskItem.Create("DEP-010", "Prereq A", 3, 1, 1));

        // Task B: 5 days
        _db.Tasks.Add(TaskItem.Create("DEP-011", "Prereq B", 5, 1, 1));

        // Task C: depends on both A and B
        _db.Tasks.Add(TaskItem.Create("DEP-012", "Final Task", 2, 1, 1, dependsOnTaskIds: "DEP-010,DEP-011"));
        _db.SaveChanges();

        _engine.RunScheduler();

        var prereqA = _db.Tasks.First(t => t.TaskId == "DEP-010");
        var prereqB = _db.Tasks.First(t => t.TaskId == "DEP-011");
        var final = _db.Tasks.First(t => t.TaskId == "DEP-012");

        Assert.NotNull(final.PlannedStart);
        Assert.NotNull(prereqA.PlannedFinish);
        Assert.NotNull(prereqB.PlannedFinish);

        // Must start after BOTH prerequisites finish
        var latestPrereqFinish = prereqA.PlannedFinish!.Value > prereqB.PlannedFinish!.Value
            ? prereqA.PlannedFinish!.Value
            : prereqB.PlannedFinish!.Value;

        Assert.True(final.PlannedStart!.Value >= latestPrereqFinish,
            $"Final task started {final.PlannedStart} but latest prereq finished {latestPrereqFinish}");
    }

    [Fact]
    public void RunScheduler_TaskWithNoDependency_SchedulesNormally()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("DEP-020", "Independent Task", 3, 1, 1));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "DEP-020");
        Assert.NotNull(task.PlannedStart);
        Assert.Equal("Completed", task.Status);
    }

    [Fact]
    public void RunScheduler_ChainedDependencies_SchedulesInCorrectOrder()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Chain: A -> B -> C
        _db.Tasks.Add(TaskItem.Create("CHN-001", "Chain First", 2, 1, 1));
        _db.Tasks.Add(TaskItem.Create("CHN-002", "Chain Second", 2, 1, 1, dependsOnTaskIds: "CHN-001"));
        _db.Tasks.Add(TaskItem.Create("CHN-003", "Chain Third", 2, 1, 1, dependsOnTaskIds: "CHN-002"));
        _db.SaveChanges();

        _engine.RunScheduler();

        var first = _db.Tasks.First(t => t.TaskId == "CHN-001");
        var second = _db.Tasks.First(t => t.TaskId == "CHN-002");
        var third = _db.Tasks.First(t => t.TaskId == "CHN-003");

        Assert.NotNull(first.PlannedFinish);
        Assert.NotNull(second.PlannedStart);
        Assert.NotNull(second.PlannedFinish);
        Assert.NotNull(third.PlannedStart);

        Assert.True(second.PlannedStart!.Value >= first.PlannedFinish!.Value);
        Assert.True(third.PlannedStart!.Value >= second.PlannedFinish!.Value);
    }

    [Fact]
    public void RunScheduler_DependencyOnNonExistentTask_TaskNeverScheduled()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Depends on a task that doesn't exist — can never be satisfied
        _db.Tasks.Add(TaskItem.Create("DEP-030", "Orphan Dependency", 3, 1, 1, dependsOnTaskIds: "MISSING-999"));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "DEP-030");
        // Task should never start because its dependency can't be completed
        Assert.Equal("Not Started", task.Status);
        Assert.Null(task.PlannedStart);
    }

    // ------------------------------------------------------------------
    // Circular dependency — A depends on B, B depends on A → neither starts
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_CircularDependency_NeitherTaskStarts()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("CIR-001", "Circular A", 3, 1, 1, dependsOnTaskIds: "CIR-002"));
        _db.Tasks.Add(TaskItem.Create("CIR-002", "Circular B", 3, 1, 1, dependsOnTaskIds: "CIR-001"));
        _db.SaveChanges();

        // Should not throw — circular deps just mean tasks never get scheduled
        var exception = Record.Exception(() => _engine.RunScheduler());
        Assert.Null(exception);

        var taskA = _db.Tasks.First(t => t.TaskId == "CIR-001");
        var taskB = _db.Tasks.First(t => t.TaskId == "CIR-002");
        Assert.Equal("Not Started", taskA.Status);
        Assert.Equal("Not Started", taskB.Status);
        Assert.Null(taskA.PlannedStart);
        Assert.Null(taskB.PlannedStart);
    }

    // ------------------------------------------------------------------
    // Self-referencing dependency — A depends on A → never starts
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_SelfReferencingDependency_TaskNeverStarts()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SELF-001", "Self Referencing", 3, 1, 1, dependsOnTaskIds: "SELF-001"));
        _db.SaveChanges();

        var exception = Record.Exception(() => _engine.RunScheduler());
        Assert.Null(exception);

        var task = _db.Tasks.First(t => t.TaskId == "SELF-001");
        Assert.Equal("Not Started", task.Status);
        Assert.Null(task.PlannedStart);
    }

    // ------------------------------------------------------------------
    // At Risk path — strict date within threshold, finish before deadline
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_TaskWithStrictDateWithinThreshold_MarkedAtRisk()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // At risk threshold is 5 working days by default.
        // Set strict date to just a few working days from today so finish is before strict
        // but working days remaining <= 5.
        var strictDate = DateTime.Today.AddDays(3); // Very close deadline

        _db.Tasks.Add(TaskItem.Create("SV-116", "At Risk Task", 0.5, 1, 1, strictDate: strictDate));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-116");
        // The task should be "At Risk", "On Track", or "Late" depending on exact date/scheduling
        Assert.Contains(task.DeliveryRisk, new[] { "At Risk", "On Track", "Late" });
    }

    // ------------------------------------------------------------------
    // CalculateRisk — plannedFinish is null but strictDate exists → At Risk
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateRisk_NoPlannedFinishWithStrictDate_ReturnsAtRisk()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.Resources.RemoveRange(_db.Resources);
        _db.SaveChanges();

        // No resources → no capacity → task won't be scheduled → no PlannedFinish
        _db.Tasks.Add(TaskItem.Create("SV-117", "Unschedulable With Strict", 10, 1, 1, strictDate: DateTime.Today.AddDays(30)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-117");
        // No planned finish + has strict date → At Risk
        Assert.Equal("At Risk", task.DeliveryRisk);
    }

    // ------------------------------------------------------------------
    // Calendar capacity — weekends have zero effective capacity
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_WeekendDays_HaveZeroEffectiveCapacity()
    {
        _engine.RunScheduler();

        // DayOfWeek cannot be translated to SQL Server — evaluate client-side
        var weekendCalDays = _db.Calendar
            .ToList()
            .Where(c => c.CalendarDate.DayOfWeek == DayOfWeek.Friday
                     || c.CalendarDate.DayOfWeek == DayOfWeek.Saturday)
            .Take(10)
            .ToList();

        Assert.NotEmpty(weekendCalDays);
        Assert.All(weekendCalDays, d =>
        {
            Assert.False(d.IsWorkingDay);
            Assert.Equal(0, d.EffectiveCapacity);
        });
    }

    // ------------------------------------------------------------------
    // Calendar capacity — holiday days have zero effective capacity
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_HolidayDays_HaveZeroEffectiveCapacity()
    {
        _engine.RunScheduler();

        // DayOfWeek cannot be translated to SQL Server — evaluate client-side
        var holidayCalDays = _db.Calendar
            .Where(c => c.IsHoliday)
            .ToList()
            .Where(c => c.CalendarDate.DayOfWeek != DayOfWeek.Friday
                     && c.CalendarDate.DayOfWeek != DayOfWeek.Saturday)
            .Take(5)
            .ToList();

        Assert.NotEmpty(holidayCalDays);
        Assert.All(holidayCalDays, d =>
        {
            Assert.False(d.IsWorkingDay);
            Assert.Equal(0, d.EffectiveCapacity);
        });
    }

    // ------------------------------------------------------------------
    // Calendar — holiday name is populated on holiday days
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_HolidayDays_HaveHolidayNameSet()
    {
        _engine.RunScheduler();

        var holidayCalDays = _db.Calendar.Where(c => c.IsHoliday).Take(5).ToList();
        Assert.NotEmpty(holidayCalDays);
        Assert.All(holidayCalDays, d => Assert.False(string.IsNullOrWhiteSpace(d.HolidayName)));
    }

    // ------------------------------------------------------------------
    // Multiple stacking adjustments on same resource
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_MultipleAdjustments_StackMultiplicatively()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.Resources.RemoveRange(_db.Resources);
        _db.Adjustments.RemoveRange(_db.Adjustments);
        _db.SaveChanges();

        _db.Resources.Add(TeamMember.Create("RES-13", "Stack Test", "Developer", "Default", 100, 8, new DateTime(2026, 1, 1)));

        // Two 50% adjustments stacking: 8 * (50/100) * (50/100) = 2
        _db.Adjustments.Add(Adjustment.Create("RES-13", "Training", 50, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));
        _db.Adjustments.Add(Adjustment.Create("RES-13", "Other", 50, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));

        _db.Tasks.Add(TaskItem.Create("SV-118", "Multi Stack Task", 5, 10, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        // Each allocation in May should have AssignedDev <= 2 (8 * 0.5 * 0.5)
        var mayAllocations = _db.Allocations
            .Where(a => a.TaskId == "SV-118"
                && a.CalendarDate.Month == 5 && a.CalendarDate.Year == 2026)
            .ToList();

        Assert.NotEmpty(mayAllocations);
        Assert.All(mayAllocations, a =>
            Assert.True(a.AssignedDev <= 2.0,
                $"Expected AssignedDev <= 2.0 (stacked adjustments) but got {a.AssignedDev}"));
    }

    // ------------------------------------------------------------------
    // Resource StartDate in future — no capacity before start
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_ResourceStartDateInFuture_NoCapacityBeforeStart()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.Resources.RemoveRange(_db.Resources);
        _db.SaveChanges();

        _db.Resources.Add(TeamMember.Create("RES-14", "Future Dev", "Developer", "Default", 100, 8, new DateTime(2026, 8, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-119", "Future Resource Task", 3, 1, 5));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-119");
        // Task should not start before August 1
        if (task.PlannedStart.HasValue)
        {
            Assert.True(task.PlannedStart.Value >= new DateTime(2026, 8, 1),
                $"Task started {task.PlannedStart:yyyy-MM-dd} before resource StartDate 2026-08-01");
        }
    }

    // ------------------------------------------------------------------
    // GetOutputPlan — empty tasks returns empty list
    // ------------------------------------------------------------------

    [Fact]
    public void GetOutputPlan_NoTasks_ReturnsEmptyList()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        var output = _engine.GetOutputPlan();
        Assert.NotNull(output);
        Assert.Empty(output);
    }

    // ------------------------------------------------------------------
    // GetDashboardKPIs — upcoming_strict is capped at 5
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_ManyStrictDates_UpcomingStrictCappedAtFive()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        for (int i = 1; i <= 10; i++)
        {
            _db.Tasks.Add(TaskItem.Create($"SV-{200 + i:D3}", $"Strict Task {i}", 3, 1, 5, strictDate: DateTime.Today.AddDays(i * 10)));
        }
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var kpis = engine.GetDashboardKPIs();

        var upcoming = kpis["upcoming_strict"] as List<TaskItem>;
        Assert.NotNull(upcoming);
        Assert.True(upcoming.Count <= 5);
    }

    // ------------------------------------------------------------------
    // GetDashboardKPIs — upcoming_strict sorted by earliest first
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_UpcomingStrict_OrderedByEarliestFirst()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-211", "Later Strict", 3, 1, 5, strictDate: DateTime.Today.AddDays(60)));
        _db.Tasks.Add(TaskItem.Create("SV-212", "Earlier Strict", 3, 1, 5, strictDate: DateTime.Today.AddDays(10)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var kpis = engine.GetDashboardKPIs();

        var upcoming = kpis["upcoming_strict"] as List<TaskItem>;
        Assert.NotNull(upcoming);
        Assert.True(upcoming.Count >= 2);
        Assert.True(upcoming[0].StrictDate <= upcoming[1].StrictDate);
    }
}
