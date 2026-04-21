using Microsoft.EntityFrameworkCore;
using SoftwareDeliveryPlanner.Infrastructure.Data;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Services;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

[Collection(DatabaseCollection.Name)]
public class SchedulingEngineTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<(string, double, double)> B(double dev, double qa = 1) => TestDatabaseHelper.MakeBreakdown(dev, qa);

    public SchedulingEngineTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);
        _engine = new SchedulingEngine(_db, TimeProvider.System);
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
        // 2026-09-23 is National Day (Wednesday) - a seeded single-day holiday on a working day
        var nationalDay = new DateTime(2026, 9, 23);
        Assert.False(_engine.IsWorkingDay(nationalDay));
    }

    [Fact]
    public void IsWorkingDay_MultiDayHoliday_MiddleDate_ReturnsFalse()
    {
        // 2026-03-31 is in Eid Al-Fitr range (Mar 30 - Apr 2) and is a Tuesday (working day)
        var eidMiddle = new DateTime(2026, 3, 31);
        Assert.False(_engine.IsWorkingDay(eidMiddle));
    }

    [Fact]
    public void IsWorkingDay_MultiDayHoliday_StartDate_ReturnsFalse()
    {
        // 2026-03-30 is the start of Eid Al-Fitr (Monday)
        var eidStart = new DateTime(2026, 3, 30);
        Assert.False(_engine.IsWorkingDay(eidStart));
    }

    [Fact]
    public void IsWorkingDay_MultiDayHoliday_EndDate_ReturnsFalse()
    {
        // 2026-04-02 is the end of Eid Al-Fitr (Thursday)
        var eidEnd = new DateTime(2026, 4, 2);
        Assert.False(_engine.IsWorkingDay(eidEnd));
    }

    [Fact]
    public void IsWorkingDay_DayAfterHolidayRange_ReturnsTrue()
    {
        // 2026-04-05 is the Sunday after Eid (Apr 2 was end), should be working
        var dayAfter = new DateTime(2026, 4, 5);
        Assert.True(_engine.IsWorkingDay(dayAfter));
    }

    #endregion

    #region IsWorkingDay edge cases

    [Fact]
    public void IsWorkingDay_HolidayOnWeekend_ReturnsFalse()
    {
        // Labour Day 2026-05-01 is a Friday (weekend) — both weekend AND holiday
        var labourDay = new DateTime(2026, 5, 1);
        Assert.False(_engine.IsWorkingDay(labourDay));
    }

    [Fact]
    public void IsWorkingDay_DateBeforeAllHolidays_ReturnsTrue()
    {
        // 2026-01-05 is a Monday (working day), and no seeded holiday falls on this date
        // (New Year's Day Jan 1 is seeded; next holiday is Founding Day Feb 22)
        var jan5 = new DateTime(2026, 1, 5);
        Assert.True(_engine.IsWorkingDay(jan5));
    }

    [Fact]
    public void IsWorkingDay_NewYearsDay2027_ReturnsTrue()
    {
        // 2027-01-04 is a Monday (working day) and not a seeded holiday
        var date = new DateTime(2027, 1, 4);
        Assert.True(_engine.IsWorkingDay(date));
    }

    #endregion

    #region GetHolidayForDate Tests

    [Fact]
    public void GetHolidayForDate_WithinRange_ReturnsHoliday()
    {
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 3, 31));
        Assert.NotNull(holiday);
        Assert.Contains("الفطر", holiday.HolidayName);
    }

    [Fact]
    public void GetHolidayForDate_OutsideRange_ReturnsNull()
    {
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 5, 4));
        Assert.Null(holiday);
    }

    #endregion

    #region GetHolidayForDate edge cases

    [Fact]
    public void GetHolidayForDate_StartDate_ReturnsHoliday()
    {
        // First day of Eid Al-Fitr: Mar 30, 2026
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 3, 30));
        Assert.NotNull(holiday);
        Assert.Contains("الفطر", holiday.HolidayName);
    }

    [Fact]
    public void GetHolidayForDate_EndDate_ReturnsHoliday()
    {
        // Last day of Eid Al-Fitr: Apr 2, 2026
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 4, 2));
        Assert.NotNull(holiday);
        Assert.Contains("الفطر", holiday.HolidayName);
    }

    [Fact]
    public void GetHolidayForDate_DayBefore_ReturnsNull()
    {
        // Day before Eid Al-Fitr start: Mar 29, 2026
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 3, 29));
        Assert.Null(holiday);
    }

    [Fact]
    public void GetHolidayForDate_SingleDayHoliday_ReturnsHoliday()
    {
        // National Day: Sep 23, 2026 (single-day holiday)
        var holiday = _engine.GetHolidayForDate(new DateTime(2026, 9, 23));
        Assert.NotNull(holiday);
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

    #region GetWorkingDaysBetween with holidays

    [Fact]
    public void GetWorkingDaysBetween_SpanningHoliday_ExcludesHoliday()
    {
        // Sep 21 (Mon) to Sep 24 (Thu) 2026 — Sep 23 is National Day (holiday)
        // Working days: Sep 21 (Mon), Sep 22 (Tue), Sep 24 (Thu) = 3
        var start = new DateTime(2026, 9, 21);
        var end = new DateTime(2026, 9, 24);
        var result = _engine.GetWorkingDaysBetween(start, end);
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_SpanningMultiDayHoliday_ExcludesAll()
    {
        // Mar 29 (Sun) to Apr 5 (Sun) 2026
        // Eid Al-Fitr: Mar 30 (Mon) - Apr 2 (Thu) — 4 holiday days on working days
        // Working days in range: Mar 29 (Sun), Apr 5 (Sun) = 2 working days
        // Apr 3 (Fri) and Apr 4 (Sat) are weekend
        var start = new DateTime(2026, 3, 29);
        var end = new DateTime(2026, 4, 5);
        var result = _engine.GetWorkingDaysBetween(start, end);
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetWorkingDaysBetween_EntirelyWithinHoliday_ReturnsZero()
    {
        // Mar 30 (Mon) to Apr 2 (Thu) 2026 — entirely within Eid Al-Fitr
        var start = new DateTime(2026, 3, 30);
        var end = new DateTime(2026, 4, 2);
        var result = _engine.GetWorkingDaysBetween(start, end);
        Assert.Equal(0, result);
    }

    #endregion

    #region CalculateSchedulingRank Tests

    [Fact]
    public void CalculateSchedulingRank_StrictDateTask_HigherPriority()
    {
        var task1 = TaskItem.Create("TST-01", "Test", 1, 5, B(1), strictDate: new DateTime(2026, 6, 1));
        var task2 = TaskItem.Create("TST-02", "Test", 1, 3, B(1), strictDate: new DateTime(2026, 6, 1));

        var rank1 = GetPrivateRank(task1);
        var rank2 = GetPrivateRank(task2);

        Assert.True(rank1 < rank2);
    }

    [Fact]
    public void CalculateSchedulingRank_StrictDate_EarlierDateHigherPriority()
    {
        var task1 = TaskItem.Create("TST-01", "Test", 1, 5, B(1), strictDate: new DateTime(2026, 5, 1));
        var task2 = TaskItem.Create("TST-02", "Test", 1, 5, B(1), strictDate: new DateTime(2026, 6, 1));

        var rank1 = GetPrivateRank(task1);
        var rank2 = GetPrivateRank(task2);

        Assert.True(rank1 < rank2); // earlier deadline → lower rank → scheduled first
    }

    [Fact]
    public void CalculateSchedulingRank_NoStrictDate_LowerPriorityThanStrict()
    {
        var taskWithStrict = TaskItem.Create("TST-01", "Test", 1, 5, B(1), strictDate: new DateTime(2026, 6, 1));
        var taskWithoutStrict = TaskItem.Create("TST-02", "Test", 1, 5, B(1));

        var rankWithStrict = GetPrivateRank(taskWithStrict);
        var rankWithoutStrict = GetPrivateRank(taskWithoutStrict);

        Assert.True(rankWithStrict < rankWithoutStrict);
    }

    private int GetPrivateRank(TaskItem task)
    {
        int priorityComponent = (11 - task.Priority) * 10;
        if (!task.StrictDate.HasValue)
            return int.MaxValue / 2 + priorityComponent;
        int daysSinceRef = (int)(task.StrictDate.Value.Date - new DateTime(2000, 1, 1)).TotalDays;
        return daysSinceRef * 100 + priorityComponent;
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

        _db.Tasks.Add(TaskItem.Create("TST-001", "Test Service", 1, 5, B(5)));
        _db.SaveChanges();

        var result = _engine.RunScheduler();

        Assert.Contains("successfully scheduled", result, StringComparison.OrdinalIgnoreCase);
        var allocations = _db.Allocations.Where(a => a.TaskId == "TST-001").ToList();
        Assert.True(allocations.Count > 0);
    }

    [Fact]
    public void RunScheduler_MultipleTasks_SchedulesByPriority()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("TL-001", "Low Priority", 1, 10, B(3)));
        _db.Tasks.Add(TaskItem.Create("TH-001", "High Priority", 1, 1, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var lowTask = _db.Tasks.First(t => t.TaskId == "TL-001");
        var highTask = _db.Tasks.First(t => t.TaskId == "TH-001");

        Assert.True(highTask.PlannedStart <= lowTask.PlannedStart);
    }

    [Fact]
    public void RunScheduler_StrictDateTask_SchedulesBeforeDeadline()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        var futureDate = DateTime.Today.AddDays(60);

        _db.Tasks.Add(TaskItem.Create("TS-001", "Strict Deadline", 1, 5, B(10), strictDate: futureDate));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "TS-001");
        Assert.NotNull(task.PlannedFinish);
    }

    [Fact]
    public void RunScheduler_UpdatesTaskDates()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("TD-001", "Date Test", 1, 5, B(5)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "TD-001");
        Assert.NotNull(task.PlannedStart);
        Assert.NotNull(task.PlannedFinish);
        Assert.True(task.Duration > 0);
    }

    #endregion

    #region RunScheduler result format

    [Fact]
    public void RunScheduler_WithDefaultData_ReturnsAllocationsCount()
    {
        var result = _engine.RunScheduler();
        Assert.Contains("allocations", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunScheduler_SingleTask_StatusIsCompleted()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.Allocations.RemoveRange(_db.Allocations);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("SM-001", "Small Task", 1, 5, B(1)));
        _db.SaveChanges();

        _engine.RunScheduler();

        var task = _db.Tasks.First(t => t.TaskId == "SM-001");
        Assert.Equal("Completed", task.Status);
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

    #region GetDashboardKPIs edge cases

    [Fact]
    public void GetDashboardKPIs_ContainsTotalEstimation()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("total_estimation"));
    }

    [Fact]
    public void GetDashboardKPIs_ContainsAvgAssigned()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("avg_assigned"));
    }

    [Fact]
    public void GetDashboardKPIs_ContainsStrictCount()
    {
        var kpis = _engine.GetDashboardKPIs();
        Assert.True(kpis.ContainsKey("strict_count"));
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
            Assert.NotNull(first.TaskId);
            Assert.NotNull(first.ServiceName);
            Assert.NotNull(first.Status);
        }
    }

    [Fact]
    public void GetOutputPlan_IsOrderedBySchedulingRank()
    {
        var output = _engine.GetOutputPlan();
        for (int i = 1; i < output.Count; i++)
        {
            var prev = output[i - 1].Num;
            var curr = output[i].Num;
            Assert.True(prev < curr);
        }
    }

    #endregion
}

// ============================================================
// Mon-Fri working week configuration tests
// ============================================================

[Collection(DatabaseCollection.Name)]
public class SchedulingEngineMonFriTests : IDisposable
{
    private readonly PlannerDbContext _db;
    private readonly SchedulingEngine _engine;

    private static List<(string, double, double)> B(double dev, double qa = 1) => TestDatabaseHelper.MakeBreakdown(dev, qa);

    public SchedulingEngineMonFriTests(SqlServerFixture fixture)
    {
        var (options, _) = TestDatabaseHelper.CreateOptions(fixture);
        _db = new PlannerDbContext(options);

        // Change working week to Mon-Fri
        var weekSetting = _db.Settings.First(s => s.Key == "working_week");
        weekSetting.Value = "mon_fri";
        _db.SaveChanges();

        _engine = new SchedulingEngine(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void IsWorkingDay_Friday_ReturnsTrue_InMonFriConfig()
    {
        // Friday May 8, 2026 — should be a working day in Mon-Fri
        var friday = new DateTime(2026, 5, 8);
        Assert.True(_engine.IsWorkingDay(friday));
    }

    [Fact]
    public void IsWorkingDay_Saturday_ReturnsFalse_InMonFriConfig()
    {
        // Saturday May 9, 2026 — weekend in Mon-Fri
        var saturday = new DateTime(2026, 5, 9);
        Assert.False(_engine.IsWorkingDay(saturday));
    }

    [Fact]
    public void IsWorkingDay_Sunday_ReturnsFalse_InMonFriConfig()
    {
        // Sunday May 10, 2026 — weekend in Mon-Fri
        var sunday = new DateTime(2026, 5, 10);
        Assert.False(_engine.IsWorkingDay(sunday));
    }

    [Fact]
    public void IsWorkingDay_Monday_ReturnsTrue_InMonFriConfig()
    {
        var monday = new DateTime(2026, 5, 4);
        Assert.True(_engine.IsWorkingDay(monday));
    }

    [Fact]
    public void GetWorkingDaysBetween_OneWeek_ReturnsFive_InMonFriConfig()
    {
        // Mon May 4 to Sun May 10, 2026 — Mon-Fri = 5 working days
        var start = new DateTime(2026, 5, 4);
        var end = new DateTime(2026, 5, 10);
        var result = _engine.GetWorkingDaysBetween(start, end);
        Assert.Equal(5, result);
    }

    [Fact]
    public void RunScheduler_MonFriConfig_WeekendDaysHaveZeroCapacity()
    {
        _db.Tasks.RemoveRange(_db.Tasks);
        _db.SaveChanges();

        _db.Tasks.Add(TaskItem.Create("MF-001", "Mon-Fri Test", 1, 5, B(3)));
        _db.SaveChanges();

        _engine.RunScheduler();

        // DayOfWeek cannot be translated to SQL Server — evaluate client-side
        var allCalDays = _db.Calendar.ToList();

        // Saturday and Sunday should have zero capacity
        var weekendDays = allCalDays
            .Where(c => c.CalendarDate.DayOfWeek == DayOfWeek.Saturday
                     || c.CalendarDate.DayOfWeek == DayOfWeek.Sunday)
            .Take(10)
            .ToList();

        Assert.NotEmpty(weekendDays);
        Assert.All(weekendDays, d =>
        {
            Assert.False(d.IsWorkingDay);
            Assert.Equal(0, d.EffectiveCapacity);
        });

        // Friday should be working
        var fridayDays = allCalDays
            .Where(c => c.CalendarDate.DayOfWeek == DayOfWeek.Friday && !c.IsHoliday)
            .Take(5)
            .ToList();

        Assert.NotEmpty(fridayDays);
        Assert.All(fridayDays, d => Assert.True(d.IsWorkingDay));
    }
}
