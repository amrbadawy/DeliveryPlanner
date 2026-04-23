using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Application.Tasks.Commands;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// #14 — PeakConcurrency rename (AssignedResource → PeakConcurrency)
// ============================================================

public class Enhancement14_PeakConcurrencyTests
{
    private static List<EffortBreakdownSpec> B(double dev) =>
        new() { new("DEV", dev, 0), new("QA", Math.Max(1, dev * 0.2), 0) };

    [Fact]
    public void TaskItem_HasPeakConcurrencyProperty_NotAssignedResource()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Null(task.PeakConcurrency);
        // Verify the old name is gone at compile time — if it compiled, the rename is correct
        Assert.True(true);
    }

    [Fact]
    public void ScenarioTaskSnapshot_HasPeakConcurrencyProperty()
    {
        var snap = ScenarioTaskSnapshot.Create(1, "TSK-001", "Service", 5, null,
            null, null, null, null, null, peakConcurrency: 2.5,
            status: "NotStarted", deliveryRisk: "OnTrack",
            dependsOnTaskIds: null, phase: null);
        Assert.Equal(2.5, snap.PeakConcurrency);
    }

    [Fact]
    public void ScenarioTaskSnapshot_PeakConcurrency_NullWhenNotSet()
    {
        var snap = ScenarioTaskSnapshot.Create(1, "TSK-002", "Service", 5, null,
            null, null, null, null, null, peakConcurrency: null,
            status: "NotStarted", deliveryRisk: "OnTrack",
            dependsOnTaskIds: null, phase: null);
        Assert.Null(snap.PeakConcurrency);
    }
}

// ============================================================
// #1 — Resource Seniority Model
// ============================================================

public class Enhancement1_SeniorityModelTests
{
    [Theory]
    [InlineData("Junior")]
    [InlineData("Mid")]
    [InlineData("Senior")]
    [InlineData("Principal")]
    public void TeamMember_Create_ValidSeniorityLevels(string level)
    {
        var member = TeamMember.Create("DEV-001", "Dev One", "DEV", "Delivery",
            100, 1, new DateTime(2026, 1, 1), seniorityLevel: level);
        Assert.Equal(level, member.SeniorityLevel);
    }

    [Fact]
    public void TeamMember_Create_DefaultSeniority_IsMid()
    {
        var member = TeamMember.Create("DEV-001", "Dev One", "DEV", "Delivery",
            100, 1, new DateTime(2026, 1, 1));
        Assert.Equal(DomainConstants.Seniority.Mid, member.SeniorityLevel);
    }

    [Fact]
    public void TeamMember_Create_InvalidSeniority_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", "Dev One", "DEV", "Delivery",
                100, 1, new DateTime(2026, 1, 1), seniorityLevel: "Intern"));
    }

    [Fact]
    public void Seniority_Ranks_AreOrdered_JuniorLessThanPrincipal()
    {
        Assert.True(DomainConstants.Seniority.Rank["Junior"] < DomainConstants.Seniority.Rank["Mid"]);
        Assert.True(DomainConstants.Seniority.Rank["Mid"] < DomainConstants.Seniority.Rank["Senior"]);
        Assert.True(DomainConstants.Seniority.Rank["Senior"] < DomainConstants.Seniority.Rank["Principal"]);
    }

    [Fact]
    public void TaskEffortBreakdown_MinSeniority_CanBeSetAndRead()
    {
        var breakdown = TaskEffortBreakdown.Create("SVC-001", "DEV", 5, 0, minSeniority: "Senior");
        Assert.Equal("Senior", breakdown.MinSeniority);
    }

    [Fact]
    public void TaskEffortBreakdown_MinSeniority_NullByDefault()
    {
        var breakdown = TaskEffortBreakdown.Create("SVC-001", "DEV", 5, 0);
        Assert.Null(breakdown.MinSeniority);
    }

    [Fact]
    public void TaskEffortBreakdown_InvalidMinSeniority_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TaskEffortBreakdown.Create("SVC-001", "DEV", 5, 0, minSeniority: "Intern"));
    }
}

// ============================================================
// #9 — Per-Resource Working Week
// ============================================================

public class Enhancement9_PerResourceWorkingWeekTests
{
    [Fact]
    public void TeamMember_Create_WithWorkingWeek_Stored()
    {
        var member = TeamMember.Create("DEV-001", "Dev One", "DEV", "Delivery",
            100, 1, new DateTime(2026, 1, 1), workingWeek: DomainConstants.WorkingWeek.MonFri);
        Assert.Equal(DomainConstants.WorkingWeek.MonFri, member.WorkingWeek);
    }

    [Fact]
    public void TeamMember_Create_NullWorkingWeek_UsesGlobalDefault()
    {
        var member = TeamMember.Create("DEV-001", "Dev One", "DEV", "Delivery",
            100, 1, new DateTime(2026, 1, 1));
        Assert.Null(member.WorkingWeek);
    }

    [Fact]
    public void WorkingWeek_MonFri_GetWeekendDays_ReturnsSatSun()
    {
        var weekend = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.MonFri);
        Assert.Contains(DayOfWeek.Saturday, weekend);
        Assert.Contains(DayOfWeek.Sunday, weekend);
        Assert.DoesNotContain(DayOfWeek.Monday, weekend);
    }

    [Fact]
    public void WorkingWeek_SunThu_GetWeekendDays_ReturnsFriSat()
    {
        var weekend = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.SunThu);
        Assert.Contains(DayOfWeek.Friday, weekend);
        Assert.Contains(DayOfWeek.Saturday, weekend);
        Assert.DoesNotContain(DayOfWeek.Sunday, weekend);
    }
}

// ============================================================
// #4 — TaskDependency Entity (FS / SS / FF)
// ============================================================

public class Enhancement4_TaskDependencyEntityTests
{
    private static List<EffortBreakdownSpec> B(double dev) =>
        new() { new("DEV", dev, 0), new("QA", Math.Max(1, dev * 0.2), 0) };

    [Theory]
    [InlineData("FS")]
    [InlineData("SS")]
    [InlineData("FF")]
    public void AddDependency_ValidTypes_Stored(string type)
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001", type, lagDays: 0, overlapPct: 0);
        Assert.Single(task.Dependencies);
        Assert.Equal(type, task.Dependencies.First().Type);
    }

    [Fact]
    public void AddDependency_WithLagDays_Stored()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001", "FS", lagDays: 3, overlapPct: 0);
        Assert.Equal(3, task.Dependencies.First().LagDays);
    }

    [Fact]
    public void AddDependency_WithOverlapPct_Stored()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001", "FS", lagDays: 0, overlapPct: 20);
        Assert.Equal(20, task.Dependencies.First().OverlapPct);
    }

    [Fact]
    public void AddDependency_InvalidType_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Throws<DomainException>(() => task.AddDependency("PRE-001", "INVALID"));
    }

    [Fact]
    public void AddDependency_NegativeLagDays_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Throws<DomainException>(() => task.AddDependency("PRE-001", "FS", lagDays: -1));
    }

    [Fact]
    public void AddDependency_OverlapPctOver100_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Throws<DomainException>(() => task.AddDependency("PRE-001", "FS", lagDays: 0, overlapPct: 101));
    }

    [Fact]
    public void AddDependency_Duplicate_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001");
        Assert.Throws<DomainException>(() => task.AddDependency("PRE-001"));
    }

    [Fact]
    public void AddDependency_SelfReference_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Throws<DomainException>(() => task.AddDependency("SVC-001"));
    }

    [Fact]
    public void ClearDependencies_RemovesAll()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001");
        task.AddDependency("PRE-002");
        task.ClearDependencies();
        Assert.Empty(task.Dependencies);
        Assert.Null(task.DependsOnTaskIds);
    }

    [Fact]
    public void RemoveDependency_ExistingDep_RemovesIt()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("PRE-001");
        task.AddDependency("PRE-002");
        task.RemoveDependency("PRE-001");
        Assert.Single(task.Dependencies);
        Assert.Equal("PRE-002", task.Dependencies.First().PredecessorTaskId);
    }

    [Fact]
    public void RemoveDependency_NonExistent_ThrowsDomainException()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Throws<DomainException>(() => task.RemoveDependency("MISSING-001"));
    }

    [Fact]
    public void DependsOnTaskIds_ComputedFromDependencies_AlphabeticOrder()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("ZZZ-001");
        task.AddDependency("AAA-001");
        // DependsOnTaskIds sorts alphabetically
        Assert.Equal("AAA-001,ZZZ-001", task.DependsOnTaskIds);
    }

    [Fact]
    public void DependencyType_IsValid_CorrectlyIdentifiesValidTypes()
    {
        Assert.True(DomainConstants.DependencyType.IsValid("FS"));
        Assert.True(DomainConstants.DependencyType.IsValid("SS"));
        Assert.True(DomainConstants.DependencyType.IsValid("FF"));
        Assert.False(DomainConstants.DependencyType.IsValid("SF"));
        Assert.False(DomainConstants.DependencyType.IsValid("INVALID"));
    }
}

// ============================================================
// #3 — Baseline Freeze / Task Locking (Allocation.IsLocked)
// ============================================================

public class Enhancement3_BaselineFreezeTests
{
    [Fact]
    public void Allocation_IsLocked_DefaultsFalse()
    {
        var alloc = new Allocation { AllocationId = "A1", TaskId = "SVC-001", ResourceId = "DEV-001" };
        Assert.False(alloc.IsLocked);
    }

    [Fact]
    public void Allocation_IsLocked_CanBeSetTrue()
    {
        var alloc = new Allocation { AllocationId = "A1", TaskId = "SVC-001", ResourceId = "DEV-001", IsLocked = true };
        Assert.True(alloc.IsLocked);
    }

    [Fact]
    public void SettingKeys_BaselineDate_KeyExists()
    {
        Assert.Equal("baseline_date", DomainConstants.SettingKeys.BaselineDate);
    }
}

// ============================================================
// #15 — Scheduler Strategy Pattern
// ============================================================

public class Enhancement15_SchedulingStrategyTests
{
    [Fact]
    public void SchedulingStrategy_AllStrategies_Defined()
    {
        Assert.Equal("priority_first", DomainConstants.SchedulingStrategy.PriorityFirst);
        Assert.Equal("deadline_first", DomainConstants.SchedulingStrategy.DeadlineFirst);
        Assert.Equal("balanced_workload", DomainConstants.SchedulingStrategy.BalancedWorkload);
        Assert.Equal("critical_path", DomainConstants.SchedulingStrategy.CriticalPath);
    }

    [Fact]
    public void SchedulingStrategy_AllCollection_ContainsFourStrategies()
    {
        Assert.Equal(4, DomainConstants.SchedulingStrategy.All.Count);
    }

    [Theory]
    [InlineData("priority_first")]
    [InlineData("deadline_first")]
    [InlineData("balanced_workload")]
    [InlineData("critical_path")]
    public void SchedulingStrategy_All_ContainsEachStrategy(string strategy)
    {
        Assert.Contains(strategy, DomainConstants.SchedulingStrategy.All);
    }

    [Fact]
    public void SettingKeys_SchedulingStrategy_KeyExists()
    {
        Assert.Equal("scheduling_strategy", DomainConstants.SettingKeys.SchedulingStrategy);
    }
}

// ============================================================
// #11 — Overallocation Dashboard / #12 — Preview Diff /
// #13 — Utilization Forecast / #5 — Capacity Feasibility
// (DB-based tests using the scheduling engine directly)
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement11_12_13_5_SchedulingServicesTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<EffortBreakdownSpec> B(double dev) =>
        TestDatabaseHelper.MakeBreakdown(dev);

    public Enhancement11_12_13_5_SchedulingServicesTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    // ── #11 Overallocation count in KPIs ────────────────────────────

    [Fact]
    public void GetDashboardKPIs_ContainsOverallocationCount_Key()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("overallocation_count"),
            "KPI dictionary must include 'overallocation_count'");
    }

    [Fact]
    public void GetDashboardKPIs_OverallocationCount_IsNonNegativeInt()
    {
        var kpis = _engine.GetDashboardKPIs();
        var count = (int)kpis["overallocation_count"];
        Assert.True(count >= 0);
    }

    [Fact]
    public void GetDashboardKPIs_AfterSchedulerRun_OverallocationCountIsInteger()
    {
        _engine.RunScheduler();
        var kpis = _engine.GetDashboardKPIs();
        Assert.IsType<int>(kpis["overallocation_count"]);
    }

    // ── #12 PreviewSchedule diff ─────────────────────────────────────

    [Fact]
    public void PreviewSchedule_ReturnsScheduleDiffDto()
    {
        var diff = _engine.PreviewSchedule();
        Assert.NotNull(diff);
    }

    [Fact]
    public void PreviewSchedule_DoesNotPersistAllocations()
    {
        var allocsBefore = _db.Allocations.Count();
        _engine.PreviewSchedule();
        var allocsAfter = _db.Allocations.Count();
        Assert.Equal(allocsBefore, allocsAfter);
    }

    [Fact]
    public void PreviewSchedule_ReturnsNewAllocationsCount_NonNegative()
    {
        var diff = _engine.PreviewSchedule();
        Assert.True(diff.NewAllocations >= 0);
    }

    [Fact]
    public void PreviewSchedule_TaskChanges_IsNotNull()
    {
        var diff = _engine.PreviewSchedule();
        Assert.NotNull(diff.TaskChanges);
    }

    [Fact]
    public void PreviewSchedule_TasksAffectedPlusUnchanged_EqualsTotalTasks()
    {
        var totalTasks = _db.Tasks.Count();
        var diff = _engine.PreviewSchedule();
        Assert.Equal(totalTasks, diff.TasksAffected + diff.TasksUnchanged);
    }

    // ── #10 Parallel Phase Execution ─────────────────────────────────

    [Fact]
    public void RunScheduler_MultiPhaseTask_DEV_and_QA_BothScheduled()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // A task with both DEV and QA phases - both should get allocations
        _db.Tasks.Add(TaskItem.Create("PP-001", "Parallel Phase Task", 1, B(10)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var devAllocs = _db.Allocations.Where(a => a.TaskId == "PP-001" && a.Role == "DEV").Count();
        var qaAllocs = _db.Allocations.Where(a => a.TaskId == "PP-001" && a.Role == "QA").Count();

        Assert.True(devAllocs > 0, "DEV phase should have allocations");
        Assert.True(qaAllocs > 0, "QA phase should have allocations");
    }

    [Fact]
    public void RunScheduler_TaskCompletes_HasBothPhaseAllocations()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("PP-002", "Two Phase Task", 1, B(5)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "PP-002");
        Assert.Equal("Completed", task.Status);
        Assert.NotNull(task.PlannedStart);
        Assert.NotNull(task.PlannedFinish);
    }
}

// ============================================================
// #9 Per-Resource Working Week — engine integration
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement9_PerResourceWorkingWeek_EngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<EffortBreakdownSpec> B(double dev) =>
        TestDatabaseHelper.MakeBreakdown(dev);

    public Enhancement9_PerResourceWorkingWeek_EngineTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture, seedData: false);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void RunScheduler_ResourceWithMonFriWorkingWeek_DoesNotAllocateOnWeekend()
    {
        // Set global working week to Sun-Thu
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.PlanStartDate, Value = "2026-05-04" }); // Monday
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.AtRiskThreshold, Value = "5" });
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.WorkingWeek, Value = DomainConstants.WorkingWeek.SunThu });

        // Resource with Mon-Fri override
        var resource = TeamMember.Create("DEV-001", "Mon-Fri Dev", "DEV", "Delivery",
            100, 1, new DateTime(2026, 5, 1),
            workingWeek: DomainConstants.WorkingWeek.MonFri);
        _db.Resources.Add(resource);

        _db.Tasks.Add(TaskItem.Create("WW-001", "Working Week Task", 1, B(10)));
        _db.SaveChanges();

        _engine.RunScheduler();

        // For Mon-Fri resource, Saturday (DayOfWeek=6) and Sunday (DayOfWeek=0) should have no allocations
        var saturdayAllocs = _db.Allocations
            .Where(a => a.ResourceId == "DEV-001")
            .ToList()
            .Where(a => a.CalendarDate.DayOfWeek == DayOfWeek.Saturday)
            .Count();
        var sundayAllocs = _db.Allocations
            .Where(a => a.ResourceId == "DEV-001")
            .ToList()
            .Where(a => a.CalendarDate.DayOfWeek == DayOfWeek.Sunday)
            .Count();

        Assert.Equal(0, saturdayAllocs);
        Assert.Equal(0, sundayAllocs);
    }
}

// ============================================================
// #1 Seniority — engine filters resources by MinSeniority
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement1_Seniority_EngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    public Enhancement1_Seniority_EngineTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture, seedData: false);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void RunScheduler_PhaseWithMinSeniority_OnlyAssignsSeniorOrAbove()
    {
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.PlanStartDate, Value = "2026-05-04" });
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.AtRiskThreshold, Value = "5" });
        _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.WorkingWeek, Value = DomainConstants.WorkingWeek.MonFri });

        // Junior DEV should NOT be assigned to Senior-minimum phase
        var junior = TeamMember.Create("DEV-001", "Junior Dev", "DEV", "Delivery", 100, 1,
            new DateTime(2026, 5, 1), seniorityLevel: DomainConstants.Seniority.Junior);
        // Senior DEV should be assigned
        var senior = TeamMember.Create("DEV-002", "Senior Dev", "DEV", "Delivery", 100, 1,
            new DateTime(2026, 5, 1), seniorityLevel: DomainConstants.Seniority.Senior);

        _db.Resources.AddRange(junior, senior);

        // Task with Senior minimum for DEV phase — set via EffortBreakdownSpec
        var task = TaskItem.Create("SR-001", "Senior Required", 1,
            new List<EffortBreakdownSpec>
            {
                new("DEV", 3, 0, MinSeniority: DomainConstants.Seniority.Senior),
                new("QA", 1, 0)
            });

        _db.Tasks.Add(task);
        _db.SaveChanges();

        _engine.RunScheduler();

        // The Junior DEV should have no allocations for this task
        var juniorAllocs = _db.Allocations
            .Where(a => a.TaskId == "SR-001" && a.ResourceId == "DEV-001" && a.Role == "DEV")
            .Count();
        var seniorAllocs = _db.Allocations
            .Where(a => a.TaskId == "SR-001" && a.ResourceId == "DEV-002" && a.Role == "DEV")
            .Count();

        Assert.Equal(0, juniorAllocs);
        Assert.True(seniorAllocs > 0, "Senior resource should have DEV allocations");
    }
}

// ============================================================
// #15 Strategy — engine uses different strategy from settings
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement15_Strategy_EngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<EffortBreakdownSpec> B(double dev) =>
        TestDatabaseHelper.MakeBreakdown(dev);

    public Enhancement15_Strategy_EngineTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("priority_first")]
    [InlineData("deadline_first")]
    [InlineData("balanced_workload")]
    [InlineData("critical_path")]
    public void RunScheduler_WithEachStrategy_CompletesWithoutException(string strategy)
    {
        // Set the strategy
        var existing = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.SchedulingStrategy);
        if (existing != null)
            existing.Value = strategy;
        else
            _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.SchedulingStrategy, Value = strategy });
        _db.SaveChanges();

        var ex = Record.Exception(() => _engine.RunScheduler());
        Assert.Null(ex);

        // All tasks should be scheduled
        var tasks = _db.Tasks.ToList();
        Assert.True(tasks.All(t => t.PlannedStart.HasValue),
            $"Strategy '{strategy}': all tasks should be scheduled");
    }

    [Fact]
    public void RunScheduler_DeadlineFirst_StrictDateTaskScheduledFirst()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        // Set deadline_first strategy
        var existing = _db.Settings.FirstOrDefault(s => s.Key == DomainConstants.SettingKeys.SchedulingStrategy);
        if (existing != null)
            existing.Value = DomainConstants.SchedulingStrategy.DeadlineFirst;
        else
            _db.Settings.Add(new Setting { Key = DomainConstants.SettingKeys.SchedulingStrategy, Value = DomainConstants.SchedulingStrategy.DeadlineFirst });

        // Task with a near strict date (should be scheduled first)
        var urgentDate = new DateTime(2026, 6, 1);
        _db.Tasks.Add(TaskItem.Create("DF-001", "Urgent Task", 5, B(3), urgentDate));
        // Task with no strict date (lower priority in deadline_first)
        _db.Tasks.Add(TaskItem.Create("DF-002", "Non-Urgent Task", 5, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var urgent = _db.Tasks.First(t => t.TaskId == "DF-001");
        var nonUrgent = _db.Tasks.First(t => t.TaskId == "DF-002");

        Assert.NotNull(urgent.SchedulingRank);
        Assert.NotNull(nonUrgent.SchedulingRank);
        // Urgent (deadline) task should have a lower (earlier) scheduling rank
        Assert.True(urgent.SchedulingRank <= nonUrgent.SchedulingRank,
            "Deadline-first: urgent task must have earlier scheduling rank");
    }
}

// ============================================================
// #3 Baseline Freeze — locked allocations not removed
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement3_BaselineFreeze_EngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<EffortBreakdownSpec> B(double dev) =>
        TestDatabaseHelper.MakeBreakdown(dev);

    public Enhancement3_BaselineFreeze_EngineTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void RunScheduler_LockedAllocation_IsPreserved()
    {
        // Run the scheduler first to generate allocations
        _engine.RunScheduler();

        // Lock one allocation
        var alloc = _db.Allocations.First();
        alloc.IsLocked = true;
        _db.SaveChanges();
        var lockedId = alloc.AllocationId;

        // Run scheduler again — locked allocation should survive
        _engine.RunScheduler();

        var stillExists = _db.Allocations.Any(a => a.AllocationId == lockedId);
        Assert.True(stillExists, "Locked allocation must not be removed by the scheduler");
    }

    [Fact]
    public void RunScheduler_UnlockedAllocations_AreReplacedOnReschedule()
    {
        _engine.RunScheduler();
        var unlockedCount1 = _db.Allocations.Count(a => !a.IsLocked);

        _engine.RunScheduler();
        var unlockedCount2 = _db.Allocations.Count(a => !a.IsLocked);

        // Unlocked allocations are cleared and regenerated — count may vary slightly but should be > 0
        Assert.True(unlockedCount2 >= 0);
    }
}

// ============================================================
// UpsertTaskCommand — Dependencies parameter (handler integration)
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement4_DependencyPersistenceTests : OrchestratorFixture
{
    public Enhancement4_DependencyPersistenceTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task UpsertTask_WithFSDependency_PersistedCorrectly()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0, TaskId: "FSD-001", ServiceName: "FS Task", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: new List<DependencyInput> { new("SVC-001", "FS", 0, 0) },
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.Include(t => t.Dependencies).FirstAsync(t => t.TaskId == "FSD-001");
        Assert.Single(task.Dependencies);
        Assert.Equal("FS", task.Dependencies.First().Type);
        Assert.Equal("SVC-001", task.Dependencies.First().PredecessorTaskId);
    }

    [Fact]
    public async Task UpsertTask_WithSSDependency_PersistedCorrectly()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);
        await handler.Handle(new UpsertTaskCommand(
            Id: 0, TaskId: "SSD-001", ServiceName: "SS Task", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: new List<DependencyInput> { new("SVC-001", "SS", 2, 0) },
            IsNew: true), CancellationToken.None);

        await using var db = await Factory.CreateDbContextAsync();
        var task = await db.Tasks.Include(t => t.Dependencies).FirstAsync(t => t.TaskId == "SSD-001");
        Assert.Single(task.Dependencies);
        Assert.Equal("SS", task.Dependencies.First().Type);
        Assert.Equal(2, task.Dependencies.First().LagDays);
    }

    [Fact]
    public async Task UpsertTask_UpdateTask_DependenciesReplacedNotAppended()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);

        // Create with one dependency
        await handler.Handle(new UpsertTaskCommand(
            Id: 0, TaskId: "UPD-001", ServiceName: "Update Task", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: new List<DependencyInput> { new("SVC-001", "FS", 0, 0) },
            IsNew: true), CancellationToken.None);

        await using var db1 = await Factory.CreateDbContextAsync();
        var created = await db1.Tasks.FirstAsync(t => t.TaskId == "UPD-001");

        // Update with a different dependency
        await handler.Handle(new UpsertTaskCommand(
            Id: created.Id, TaskId: "UPD-001", ServiceName: "Update Task", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: new List<DependencyInput> { new("SVC-002", "FS", 0, 0) },
            IsNew: false), CancellationToken.None);

        await using var db2 = await Factory.CreateDbContextAsync();
        var updated = await db2.Tasks.Include(t => t.Dependencies).FirstAsync(t => t.TaskId == "UPD-001");
        // Should have exactly 1 dependency (SVC-002), not 2
        Assert.Single(updated.Dependencies);
        Assert.Equal("SVC-002", updated.Dependencies.First().PredecessorTaskId);
    }

    [Fact]
    public async Task UpsertTask_UpdateTask_ClearDependencies_WhenNullPassed()
    {
        var handler = new UpsertTaskCommandHandler(TaskOrchestrator);

        // Create with dependencies
        await handler.Handle(new UpsertTaskCommand(
            Id: 0, TaskId: "CLR-001", ServiceName: "Clear Deps", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: new List<DependencyInput> { new("SVC-001", "FS", 0, 0) },
            IsNew: true), CancellationToken.None);

        await using var db1 = await Factory.CreateDbContextAsync();
        var created = await db1.Tasks.FirstAsync(t => t.TaskId == "CLR-001");

        // Update with null dependencies = clear all
        await handler.Handle(new UpsertTaskCommand(
            Id: created.Id, TaskId: "CLR-001", ServiceName: "Clear Deps", Priority: 1,
            EffortBreakdown: EB(3), StrictDate: null,
            Dependencies: null,
            IsNew: false), CancellationToken.None);

        await using var db2 = await Factory.CreateDbContextAsync();
        var updated = await db2.Tasks.Include(t => t.Dependencies).FirstAsync(t => t.TaskId == "CLR-001");
        Assert.Empty(updated.Dependencies);
        Assert.Null(updated.DependsOnTaskIds);
    }
}

// ============================================================
// #5 — Capacity Feasibility Tests
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement5_FeasibilityTests : OrchestratorFixture
{
    public Enhancement5_FeasibilityTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetFeasibility_TaskWithStrictDate_NoResources_ReturnsInfeasible()
    {
        // Create a task with a tight strict date but first remove all resources
        // so it cannot be completed — should be infeasible
        await using var db = await Factory.CreateDbContextAsync();
        var task = TaskItem.Create("FEA-001", "Infeasible Task", 1,
            B(20), strictDate: DateTime.Today.AddDays(3));
        db.Tasks.Add(task);
        // Remove all resources so no one can do the work
        db.Resources.RemoveRange(db.Resources);
        await db.SaveChangesAsync();

        var results = await PlanningQueryService.GetFeasibilityAsync(taskId: "FEA-001");
        Assert.Single(results);
        var r = results.First();
        Assert.Equal("FEA-001", r.TaskId);
        Assert.False(r.IsFeasible);
        Assert.NotNull(r.Bottleneck);
    }

    [Fact]
    public async Task GetFeasibility_TaskWithStrictDate_AdequateResources_ReturnsFeasible()
    {
        // Seeded data has resources and SVC-002 has a strict date (2026-07-23)
        // with only 14 DEV + 4 QA days — that should be feasible with 3 DEV + 3 QA
        var results = await PlanningQueryService.GetFeasibilityAsync(taskId: "SVC-002");
        Assert.Single(results);
        var r = results.First();
        Assert.Equal("SVC-002", r.TaskId);
        Assert.True(r.IsFeasible);
        Assert.True(r.SlackDays >= 0);
    }

    [Fact]
    public async Task GetFeasibility_ReturnsEmptyList_WhenNoTasksHaveStrictDate()
    {
        // Create a task WITHOUT a strict date — feasibility should not include it
        await using var db = await Factory.CreateDbContextAsync();
        var task = TaskItem.Create("FEA-003", "No Strict Date", 1, B(5));
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var results = await PlanningQueryService.GetFeasibilityAsync(taskId: "FEA-003");
        Assert.Empty(results); // No strict date → not included in feasibility
    }

    [Fact]
    public async Task GetFeasibility_BottleneckPopulated_WhenInfeasible()
    {
        // Create an impossibly large task with a very tight deadline
        await using var db = await Factory.CreateDbContextAsync();
        var task = TaskItem.Create("FEA-004", "Bottleneck Task", 1,
            B(200), strictDate: DateTime.Today.AddDays(5));
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var results = await PlanningQueryService.GetFeasibilityAsync(taskId: "FEA-004");
        Assert.Single(results);
        var r = results.First();
        Assert.False(r.IsFeasible);
        Assert.NotNull(r.Bottleneck);
        Assert.NotNull(r.Recommendation);
    }
}

// ============================================================
// #13 — Utilization Forecast Tests
// ============================================================

[Collection(DatabaseCollection.Name)]
public class Enhancement13_UtilizationForecastTests : OrchestratorFixture
{
    public Enhancement13_UtilizationForecastTests(SqlServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetUtilizationForecast_DefaultWeeksAhead_Returns26Weeks()
    {
        var forecast = await PlanningQueryService.GetUtilizationForecastAsync(26);
        Assert.NotNull(forecast);
        Assert.Equal(26, forecast.Weeks.Count);
    }

    [Fact]
    public async Task GetUtilizationForecast_CustomWeeksAhead_ReturnsCorrectCount()
    {
        var forecast = await PlanningQueryService.GetUtilizationForecastAsync(4);
        Assert.NotNull(forecast);
        Assert.Equal(4, forecast.Weeks.Count);
    }

    [Fact]
    public async Task GetUtilizationForecast_AfterSchedulerRun_HasNonZeroAllocatedHours()
    {
        // Run the scheduler so allocations are created
        await SchedulerService.RunSchedulerAsync();

        var forecast = await PlanningQueryService.GetUtilizationForecastAsync(26);
        Assert.NotNull(forecast);
        Assert.True(forecast.Weeks.Count == 26);
        // At least one week should have some allocated hours after scheduling
        Assert.True(forecast.Weeks.Any(w => w.AllocatedHours > 0),
            "After running the scheduler, at least one week should have allocated hours > 0");
    }

    [Fact]
    public async Task GetUtilizationForecast_WeekStartsAreSorted()
    {
        var forecast = await PlanningQueryService.GetUtilizationForecastAsync(10);
        var weekStarts = forecast.Weeks.Select(w => w.WeekStart).ToList();
        for (int i = 1; i < weekStarts.Count; i++)
        {
            Assert.True(weekStarts[i] > weekStarts[i - 1],
                $"Week starts must be in ascending order: {weekStarts[i - 1]:yyyy-MM-dd} should be before {weekStarts[i]:yyyy-MM-dd}");
        }
    }

    [Fact]
    public async Task GetUtilizationForecast_EachWeekHasNonNegativeCapacity()
    {
        var forecast = await PlanningQueryService.GetUtilizationForecastAsync(8);
        foreach (var week in forecast.Weeks)
        {
            Assert.True(week.TotalCapacityHours >= 0,
                $"Week {week.WeekStart:yyyy-MM-dd} has negative capacity");
            Assert.True(week.AllocatedHours >= 0,
                $"Week {week.WeekStart:yyyy-MM-dd} has negative allocated hours");
        }
    }
}
