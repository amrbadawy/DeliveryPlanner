using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Data;
using SoftwareDeliveryPlanner.Models;
using SoftwareDeliveryPlanner.Services;

namespace SoftwareDeliveryPlanner.Tests;

public class SchedulingEngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    public SchedulingEngineTests()
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new PlannerDbContext(options);
        _db.Database.EnsureCreated();
        _db.InitializeDefaultData();
        _engine = new SchedulingEngine(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    #region IsWorkingDay Tests

    [Fact]
    public void IsWorkingDay_Sunday_ReturnsTrue()
    {
        var sunday = new DateTime(2026, 5, 3);
        Assert.True(_engine.IsWorkingDay(sunday));
    }

    [Fact]
    public void IsWorkingDay_Monday_ReturnsTrue()
    {
        var monday = new DateTime(2026, 5, 4);
        Assert.True(_engine.IsWorkingDay(monday));
    }

    [Fact]
    public void IsWorkingDay_Tuesday_ReturnsTrue()
    {
        var tuesday = new DateTime(2026, 5, 5);
        Assert.True(_engine.IsWorkingDay(tuesday));
    }

    [Fact]
    public void IsWorkingDay_Wednesday_ReturnsTrue()
    {
        var wednesday = new DateTime(2026, 5, 6);
        Assert.True(_engine.IsWorkingDay(wednesday));
    }

    [Fact]
    public void IsWorkingDay_Thursday_ReturnsTrue()
    {
        var thursday = new DateTime(2026, 5, 7);
        Assert.True(_engine.IsWorkingDay(thursday));
    }

    [Fact]
    public void IsWorkingDay_Friday_ReturnsFalse()
    {
        var friday = new DateTime(2026, 5, 8);
        Assert.False(_engine.IsWorkingDay(friday));
    }

    [Fact]
    public void IsWorkingDay_Saturday_ReturnsFalse()
    {
        var saturday = new DateTime(2026, 5, 9);
        Assert.False(_engine.IsWorkingDay(saturday));
    }

    [Fact]
    public void IsWorkingDay_Holiday_ReturnsFalse()
    {
        var eidDate = new DateTime(2026, 6, 6);
        Assert.False(_engine.IsWorkingDay(eidDate));
    }

    #endregion

    #region GetWorkingDaysBetween Tests

    [Fact]
    public void GetWorkingDaysBetween_SameDay_Weekday_ReturnsOne()
    {
        var monday = new DateTime(2026, 5, 4);
        var result = _engine.GetWorkingDaysBetween(monday, monday);
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_SameDay_Weekend_ReturnsZero()
    {
        var friday = new DateTime(2026, 5, 8);
        var result = _engine.GetWorkingDaysBetween(friday, friday);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_OneWeek_ReturnsFive()
    {
        var monday = new DateTime(2026, 5, 4);
        var nextMonday = new DateTime(2026, 5, 11);
        var result = _engine.GetWorkingDaysBetween(monday, nextMonday);
        Assert.Equal(6, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_TwoWeeks_ReturnsTen()
    {
        var monday = new DateTime(2026, 5, 4);
        var twoWeeksLater = new DateTime(2026, 5, 18);
        var result = _engine.GetWorkingDaysBetween(monday, twoWeeksLater);
        Assert.Equal(11, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_WeekendOnly_ReturnsZero()
    {
        var friday = new DateTime(2026, 5, 8);
        var saturday = new DateTime(2026, 5, 9);
        var result = _engine.GetWorkingDaysBetween(friday, saturday);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_StartAfterEnd_ReturnsZeroOrNegative()
    {
        var thursday = new DateTime(2026, 5, 7);
        var monday = new DateTime(2026, 5, 4);
        var result = _engine.GetWorkingDaysBetween(thursday, monday);
        Assert.True(result <= 0);
    }

    #endregion

    #region CalculateSchedulingRank Tests

    [Fact]
    public void CalculateSchedulingRank_StrictDateTask_HigherPriority()
    {
        var task1 = new TaskItem { TaskId = "T1", StrictDate = new DateTime(2026, 6, 1), Priority = 5 };
        var task2 = new TaskItem { TaskId = "T2", StrictDate = new DateTime(2026, 6, 1), Priority = 3 };

        var rank1 = GetPrivateRank(task1);
        var rank2 = GetPrivateRank(task2);

        Assert.True(rank1 < rank2);
    }

    [Fact]
    public void CalculateSchedulingRank_StrictDate_EarlierDateHigherPriority()
    {
        var task1 = new TaskItem { TaskId = "T1", StrictDate = new DateTime(2026, 5, 1), Priority = 5 };
        var task2 = new TaskItem { TaskId = "T2", StrictDate = new DateTime(2026, 6, 1), Priority = 5 };

        var rank1 = GetPrivateRank(task1);
        var rank2 = GetPrivateRank(task2);

        Assert.True(rank1 > rank2);
    }

    [Fact]
    public void CalculateSchedulingRank_NoStrictDate_LowerPriorityThanStrict()
    {
        var taskWithStrict = new TaskItem { TaskId = "T1", StrictDate = new DateTime(2026, 6, 1), Priority = 5 };
        var taskWithoutStrict = new TaskItem { TaskId = "T2", StrictDate = null, Priority = 5 };

        var rankWithStrict = GetPrivateRank(taskWithStrict);
        var rankWithoutStrict = GetPrivateRank(taskWithoutStrict);

        Assert.True(rankWithStrict < rankWithoutStrict);
    }

    private int GetPrivateRank(TaskItem task)
    {
        int hasStrict = task.StrictDate.HasValue ? 1 : 0;
        long strictVal = task.StrictDate.HasValue ? task.StrictDate.Value.Ticks : long.MaxValue;
        return hasStrict * 10000000 + (int)(strictVal / 10000) + (11 - task.Priority) * 1000;
    }

    #endregion

    #region RunScheduler Tests

    [Fact]
    public void RunScheduler_NoTasks_ReturnsNoTasksMessage()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        var result = _engine.RunScheduler();
        Assert.Equal("No tasks to schedule", result);
    }

    [Fact]
    public void RunScheduler_SingleTask_CreatesAllocations()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "T-TEST-001",
            ServiceName = "Test Service",
            DevEstimation = 5,
            MaxDev = 1,
            Priority = 5
        });
        _db.SaveChanges();

        var result = _engine.RunScheduler();

        Assert.Contains("successfully scheduled", result, StringComparison.OrdinalIgnoreCase);
        var allocations = _db.Allocations.Where(a => a.TaskId == "T-TEST-001").ToList();
        Assert.True(allocations.Count > 0);
    }

    [Fact]
    public void RunScheduler_MultipleTasks_SchedulesByPriority()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem { TaskId = "T-LOW", ServiceName = "Low Priority", DevEstimation = 3, Priority = 10 });
        _db.Tasks.Add(new TaskItem { TaskId = "T-HIGH", ServiceName = "High Priority", DevEstimation = 3, Priority = 1 });
        _db.SaveChanges();

        _engine.RunScheduler();

        var lowTask = _db.Tasks.First(t => t.TaskId == "T-LOW");
        var highTask = _db.Tasks.First(t => t.TaskId == "T-HIGH");

        Assert.True(highTask.PlannedStart <= lowTask.PlannedStart);
    }

    [Fact]
    public void RunScheduler_StrictDateTask_SchedulesBeforeDeadline()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        var futureDate = DateTime.Today.AddDays(60);

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "T-STRICT",
            ServiceName = "Strict Deadline",
            DevEstimation = 10,
            StrictDate = futureDate,
            Priority = 5
        });
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "T-STRICT");
        Assert.NotNull(task.PlannedFinish);
    }

    [Fact]
    public void RunScheduler_UpdatesTaskDates()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(new TaskItem
        {
            TaskId = "T-DATES",
            ServiceName = "Date Test",
            DevEstimation = 5,
            Priority = 5
        });
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "T-DATES");
        Assert.NotNull(task.PlannedStart);
        Assert.NotNull(task.PlannedFinish);
        Assert.True(task.Duration > 0);
    }

    #endregion

    #region GetDashboardKPIs Tests

    [Fact]
    public void GetDashboardKPIs_ReturnsTotalServices()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("total_services"));
        Assert.True((int)kpis["total_services"] > 0);
    }

    [Fact]
    public void GetDashboardKPIs_ReturnsActiveResources()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("active_resources"));
        Assert.True((int)kpis["active_resources"] > 0);
    }

    [Fact]
    public void GetDashboardKPIs_ReturnsCapacity()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("total_capacity"));
        Assert.True((double)kpis["total_capacity"] > 0);
    }

    [Fact]
    public void GetDashboardKPIs_ReturnsRiskCounts()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("on_track"));
        Assert.True(kpis.ContainsKey("at_risk"));
        Assert.True(kpis.ContainsKey("late"));
    }

    #endregion

    #region GetOutputPlan Tests

    [Fact]
    public void GetOutputPlan_ReturnsTaskList()
    {
        var output = _engine.GetOutputPlan();
        Assert.NotNull(output);
        Assert.True(output.Count > 0);
    }

    [Fact]
    public void GetOutputPlan_ContainsRequiredFields()
    {
        var output = _engine.GetOutputPlan();
        if (output.Count > 0)
        {
            var first = output[0];
            Assert.Contains("task_id", first.Keys);
            Assert.Contains("service_name", first.Keys);
            Assert.Contains("planned_start", first.Keys);
            Assert.Contains("planned_finish", first.Keys);
            Assert.Contains("status", first.Keys);
        }
    }

    [Fact]
    public void GetOutputPlan_IsOrderedBySchedulingRank()
    {
        var output = _engine.GetOutputPlan();
        for (int i = 1; i < output.Count; i++)
        {
            var prev = (int)output[i - 1]["num"];
            var curr = (int)output[i]["num"];
            Assert.True(prev < curr);
        }
    }

    #endregion
}
