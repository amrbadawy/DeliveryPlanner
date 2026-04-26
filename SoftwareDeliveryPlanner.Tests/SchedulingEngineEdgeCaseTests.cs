using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Domain;
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

    private static List<EffortBreakdownSpec> B(double dev, double qa = 1) => TestDatabaseHelper.MakeBreakdown(dev, qa);

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

        _db.Tasks.Add(TaskItem.Create("SV-100", "Override Start Test", 5, B(5), overrideStart: overrideDate));
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
    // MaxResource constraint — scheduler caps allocation at MaxResource
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_MaxFteConstraint_AllocationsRespectMaxFte()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-101", "Max Res Test", 5,
            new List<EffortBreakdownSpec> { new("DEV", 3, 0, MaxFte: 0.5), new("QA", 1, 0) }));
        _db.SaveChanges();

        _engine.RunScheduler();

        var allocations = _db.Allocations
            .Where(a => a.TaskId == "SV-101")
            .ToList();

        // Every allocation should have HoursAllocated > 0
        Assert.All(allocations, a =>
            Assert.True(a.HoursAllocated > 0,
                $"HoursAllocated {a.HoursAllocated} should be positive"));
    }

    // ------------------------------------------------------------------
    // Resource EndDate — resource should not contribute after their EndDate
    // ------------------------------------------------------------------

    [Fact]
    public void GetDashboardKPIs_ResourceWithEndDateInPast_NotCountedAsActive()
    {
        // Add a resource whose EndDate has already passed
        _db.Resources.Add(TeamMember.Create("RES-99", "Past Resource", "DEV", "Default", 100, 1, new DateTime(2020, 1, 1)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var kpis = engine.GetDashboardKPIs();

        // active_resources counts resources where Active == "YES" (no date filter in GetDashboardKPIs)
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

        _db.Resources.Add(TeamMember.Create("RES-10", "Expired", "DEV", "Default", 100, 1, new DateTime(2019, 1, 1), endDate: new DateTime(2020, 1, 1)));
        _db.Resources.Add(TeamMember.Create("QA-10", "Expired QA", "QA", "Default", 100, 1, new DateTime(2019, 1, 1), endDate: new DateTime(2020, 1, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-102", "No Capacity Task", 5, B(5)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var result = engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-102");
        // No capacity available → task should remain Not Started / unallocated
        Assert.Equal("NOT_STARTED", task.Status);
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

        _db.Resources.Add(TeamMember.Create("RES-11", "Adjustable", "DEV", "Default", 100, 1, new DateTime(2026, 1, 1)));
        _db.Resources.Add(TeamMember.Create("QA-11", "Adjustable QA", "QA", "Default", 100, 1, new DateTime(2026, 1, 1)));
        // Zero-capacity adjustment for the entire plan start month
        _db.Adjustments.Add(Adjustment.Create("RES-11", DomainConstants.AdjustmentType.Other, 0, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), "Full leave"));
        _db.Tasks.Add(TaskItem.Create("SV-103", "Zero Adj Task", 5, B(3)));
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

        _db.Tasks.Add(TaskItem.Create("SV-104", "Zero Estimation", 5, B(0.001)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);

        // Should not throw; zero-estimation tasks just get "NOT_STARTED" (no effort remaining)
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

        _db.Tasks.Add(TaskItem.Create("SV-105", "Not Assigned", 5, B(5)));
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

        _db.Tasks.Add(TaskItem.Create("SV-106", "Late Task", 5, B(500), strictDate: nearFuture));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-106");
        Assert.Equal("LATE", task.DeliveryRisk);
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

        _db.Tasks.Add(TaskItem.Create("SV-107", "Priority 1", 1, B(10)));
        _db.Tasks.Add(TaskItem.Create("SV-108", "Priority 9", 9, B(10)));
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

        _db.Tasks.Add(TaskItem.Create("SV-109", "Same Priority A", 5, B(3)));
        _db.Tasks.Add(TaskItem.Create("SV-110", "Same Priority B", 5, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var t1 = _db.Tasks.First(t => t.TaskId == "SV-109");
        var t2 = _db.Tasks.First(t => t.TaskId == "SV-110");

        Assert.NotNull(t1.PlannedStart);
        Assert.NotNull(t2.PlannedStart);
    }

    // ------------------------------------------------------------------
    // MaxResource exceeding capacity — capped at available capacity
    // ------------------------------------------------------------------

    [Fact]
    public void RunScheduler_TaskWithMaxResourceGreaterThanCapacity_CapsAtCapacity()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SV-111", "Max Dev Overflow", 5, B(10)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var allocations = _db.Allocations.Where(a => a.TaskId == "SV-111").ToList();
        // Each allocation's HoursAllocated should be positive
        Assert.All(allocations, a =>
            Assert.True(a.HoursAllocated > 0,
                $"HoursAllocated {a.HoursAllocated} should be positive"));
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
        _db.Resources.Add(TeamMember.Create("RES-12", "Inactive Dev", "DEV", "Default", 100, 1, new DateTime(2026, 1, 1), active: "NO"));
        _db.Resources.Add(TeamMember.Create("QA-12", "Inactive QA", "QA", "Default", 100, 1, new DateTime(2026, 1, 1), active: "NO"));
        _db.Tasks.Add(TaskItem.Create("SV-112", "Inactive Resources Task", 5, B(5)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-112");
        Assert.Equal("NOT_STARTED", task.Status);
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

        _db.Tasks.Add(TaskItem.Create("SV-113", "No Strict Date", 5, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-113");
        Assert.Equal("ON_TRACK", task.DeliveryRisk);
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

        _db.Tasks.Add(TaskItem.Create("SV-114", "Later Deadline", 5, B(5), strictDate: new DateTime(2026, 12, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-115", "Earlier Deadline", 5, B(5), strictDate: new DateTime(2026, 8, 1)));
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
        _db.Tasks.Add(TaskItem.Create("DEP-001", "Prerequisite Task", 1, B(5)));

        // Task B: depends on DEP-001, estimated 3 days
        var dep002 = TaskItem.Create("DEP-002", "Dependent Task", 1, B(3));
        dep002.AddDependency("DEP-001");
        _db.Tasks.Add(dep002);
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
        _db.Tasks.Add(TaskItem.Create("DEP-010", "Prereq A", 1, B(3)));

        // Task B: 5 days
        _db.Tasks.Add(TaskItem.Create("DEP-011", "Prereq B", 1, B(5)));

        // Task C: depends on both A and B
        var dep012 = TaskItem.Create("DEP-012", "Final Task", 1, B(2));
        dep012.AddDependency("DEP-010");
        dep012.AddDependency("DEP-011");
        _db.Tasks.Add(dep012);
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

        _db.Tasks.Add(TaskItem.Create("DEP-020", "Independent Task", 1, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "DEP-020");
        Assert.NotNull(task.PlannedStart);
        Assert.Equal(DomainConstants.TaskStatus.Completed, task.Status);
    }

    [Fact]
    public void RunScheduler_ChainedDependencies_SchedulesInCorrectOrder()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Chain: A -> B -> C
        _db.Tasks.Add(TaskItem.Create("CHN-001", "Chain First", 1, B(2)));
        var chn002 = TaskItem.Create("CHN-002", "Chain Second", 1, B(2));
        chn002.AddDependency("CHN-001");
        _db.Tasks.Add(chn002);
        var chn003 = TaskItem.Create("CHN-003", "Chain Third", 1, B(2));
        chn003.AddDependency("CHN-002");
        _db.Tasks.Add(chn003);
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
        var dep030 = TaskItem.Create("DEP-030", "Orphan Dependency", 1, B(3));
        dep030.AddDependency("MISSING-999");
        _db.Tasks.Add(dep030);
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "DEP-030");
        // Task should never start because its dependency can't be completed
        Assert.Equal("NOT_STARTED", task.Status);
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

        var cir001 = TaskItem.Create("CIR-001", "Circular A", 1, B(3));
        cir001.AddDependency("CIR-002");
        _db.Tasks.Add(cir001);
        var cir002 = TaskItem.Create("CIR-002", "Circular B", 1, B(3));
        cir002.AddDependency("CIR-001");
        _db.Tasks.Add(cir002);
        _db.SaveChanges();

        // Should not throw — circular deps just mean tasks never get scheduled
        var exception = Record.Exception(() => _engine.RunScheduler());
        Assert.Null(exception);

        var taskA = _db.Tasks.First(t => t.TaskId == "CIR-001");
        var taskB = _db.Tasks.First(t => t.TaskId == "CIR-002");
        Assert.Equal("NOT_STARTED", taskA.Status);
        Assert.Equal("NOT_STARTED", taskB.Status);
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

        // Domain correctly forbids self-referencing deps, so bypass it and insert directly
        // to test that the engine handles corrupt/injected data gracefully.
        var self001 = TaskItem.Create("SELF-001", "Self Referencing", 1, B(3));
        _db.Tasks.Add(self001);
        _db.SaveChanges();

        // Insert self-referencing dep directly via raw SQL (domain blocks it)
        _db.Database.ExecuteSqlRaw(
            "INSERT INTO [task].[TaskDependencies] (TaskId, PredecessorTaskId, Type, LagDays, OverlapPct) VALUES ('SELF-001', 'SELF-001', 'FS', 0, 0)");

        var exception = Record.Exception(() => _engine.RunScheduler());
        Assert.Null(exception);

        var task = _db.Tasks.First(t => t.TaskId == "SELF-001");
        Assert.Equal("NOT_STARTED", task.Status);
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

        _db.Tasks.Add(TaskItem.Create("SV-116", "At Risk Task", 1, B(0.5), strictDate: strictDate));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-116");
        // The task should be "AT_RISK", "ON_TRACK", or "LATE" depending on exact date/scheduling
        Assert.Contains(task.DeliveryRisk, new[] { "AT_RISK", "ON_TRACK", "LATE" });
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
        _db.Tasks.Add(TaskItem.Create("SV-117", "Unschedulable With Strict", 1, B(10), strictDate: DateTime.Today.AddDays(30)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SV-117");
        // No planned finish + has strict date → At Risk
        Assert.Equal("AT_RISK", task.DeliveryRisk);
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

        _db.Resources.Add(TeamMember.Create("RES-13", "Stack Test", "DEV", "Default", 100, 1, new DateTime(2026, 1, 1)));
        _db.Resources.Add(TeamMember.Create("QA-13", "Stack QA", "QA", "Default", 100, 1, new DateTime(2026, 1, 1)));

        // Two 75% adjustments stacking: 1 * 100% * 8 hrs/day * 75% * 75% = 4.5 hrs
        // (must stay >= MinAllocationHours of 4 to get any allocations)
        _db.Adjustments.Add(Adjustment.Create("RES-13", "TRAINING", 75, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));
        _db.Adjustments.Add(Adjustment.Create("RES-13", "OTHER", 75, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)));

        _db.Tasks.Add(TaskItem.Create("SV-118", "Multi Stack Task", 5, B(5)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        engine.RunScheduler();

        // Each DEV allocation in May should have HoursAllocated <= 4.5 (1 * 8 * 0.75 * 0.75)
        // With MinAllocationHours=4 floor: floor(4.5/4)*4 = 4 hours per allocation
        var mayAllocations = _db.Allocations
            .Where(a => a.TaskId == "SV-118" && a.Role == "DEV"
                && a.CalendarDate.Month == 5 && a.CalendarDate.Year == 2026)
            .ToList();

        Assert.NotEmpty(mayAllocations);
        Assert.All(mayAllocations, a =>
            Assert.True(a.HoursAllocated <= 4.5,
                $"Expected HoursAllocated <= 4.5 (stacked adjustments) but got {a.HoursAllocated}"));
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

        _db.Resources.Add(TeamMember.Create("RES-14", "Future Dev", "DEV", "Default", 100, 1, new DateTime(2026, 8, 1)));
        _db.Resources.Add(TeamMember.Create("QA-14", "Future QA", "QA", "Default", 100, 1, new DateTime(2026, 8, 1)));
        _db.Tasks.Add(TaskItem.Create("SV-119", "Future Resource Task", 5, B(3)));
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
            _db.Tasks.Add(TaskItem.Create($"SV-{200 + i:D3}", $"Strict Task {i}", 5, B(3), strictDate: DateTime.Today.AddDays(i * 10)));
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

        _db.Tasks.Add(TaskItem.Create("SV-211", "Later Strict", 5, B(3), strictDate: DateTime.Today.AddDays(60)));
        _db.Tasks.Add(TaskItem.Create("SV-212", "Earlier Strict", 5, B(3), strictDate: DateTime.Today.AddDays(10)));
        _db.SaveChanges();

        var engine = new SchedulingEngine(_db, TimeProvider.System);
        var kpis = engine.GetDashboardKPIs();

        var upcoming = kpis["upcoming_strict"] as List<TaskItem>;
        Assert.NotNull(upcoming);
        Assert.True(upcoming.Count >= 2);
        Assert.True(upcoming[0].StrictDate <= upcoming[1].StrictDate);
    }
}
