using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.SharedKernel;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Tests.Infrastructure;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// TaskItem.Create — domain factory
// ============================================================

public class TaskItemDomainTests
{
    private static List<EffortBreakdownSpec> B(double dev, double qa = 1) => TestDatabaseHelper.MakeBreakdown(dev, qa);

    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedTaskItem()
    {
        var task = TaskItem.Create("SVC-001", "My Service", 5, B(10));

        Assert.Equal("SVC-001", task.TaskId);
        Assert.Equal("My Service", task.ServiceName);
        Assert.Equal(10, task.TotalEstimationDays - 1); // DEV=10, QA=1 → total 11; just check DEV part via breakdown
        Assert.Equal(5, task.Priority);
        Assert.Null(task.StrictDate);
    }

    [Fact]
    public void Create_NormalizesTaskIdToUppercase()
    {
        var task = TaskItem.Create("svc-001", "Service", 5, B(5));
        Assert.Equal("SVC-001", task.TaskId);
    }

    [Fact]
    public void Create_WithStrictDate_SetsStrictDate()
    {
        var date = new DateTime(2026, 12, 31);
        var task = TaskItem.Create("SVC-002", "Service", 5, B(5), date);
        Assert.Equal(date, task.StrictDate);
    }

    [Fact]
    public void Create_WithDependsOnTaskIds_SetsDependencies()
    {
        var task = TaskItem.Create("SVC-003", "Service", 5, B(5));
        task.AddDependency("SVC-001");
        task.AddDependency("SVC-002");
        Assert.Equal("SVC-001,SVC-002", task.DependsOnTaskIds);
    }

    [Fact]
    public void Create_WithNullDependsOnTaskIds_SetsNull()
    {
        var task = TaskItem.Create("SVC-003", "Service", 5, B(5));
        Assert.Null(task.DependsOnTaskIds);
    }

    [Fact]
    public void Create_WithNoDependencies_ReturnsNullDependsOnTaskIds()
    {
        var task = TaskItem.Create("SVC-003", "Service", 5, B(5));
        Assert.Null(task.DependsOnTaskIds);
    }

    [Fact]
    public void AddDependency_NormalizesAndStoresPredecessorId()
    {
        var task = TaskItem.Create("SVC-003", "Service", 5, B(5));
        task.AddDependency("  SVC-001 ");
        task.AddDependency("  SVC-002  ");
        Assert.Equal("SVC-001,SVC-002", task.DependsOnTaskIds);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("123-abc")]
    [InlineData("A-1")]
    public void Create_InvalidTaskId_ThrowsDomainException(string invalidId)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(invalidId, "Service", 5, B(5)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyServiceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", name, 5, B(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Create_InvalidPriority_ThrowsDomainException(int priority)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", "Service", priority, B(5)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Create_ValidPriority_DoesNotThrow(int priority)
    {
        var task = TaskItem.Create("SVC-001", "Service", priority, B(5));
        Assert.Equal(priority, task.Priority);
    }

    [Fact]
    public void Create_TrimsServiceName()
    {
        var task = TaskItem.Create("SVC-001", "  My Service  ", 5, B(5));
        Assert.Equal("My Service", task.ServiceName);
    }

    [Fact]
    public void Create_BoundaryEstimation_SmallValue_DoesNotThrow()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(0.001));
        Assert.True(task.TotalEstimationDays > 0);
    }

    [Fact]
    public void Create_LargeEstimation_DoesNotThrow()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(10000));
        Assert.True(task.TotalEstimationDays >= 10000);
    }

    [Fact]
    public void Create_NullServiceName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TaskItem.Create("SVC-001", null!, 5, B(5)));
    }
}

// ============================================================
// TeamMember.Create — domain factory
// ============================================================

public class TeamMemberDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedTeamMember()
    {
        var member = TeamMember.Create(
            "DEV-001", "Alice", "DEV", "Delivery",
            100, 1.0, DateTime.Today);

        Assert.Equal("DEV-001", member.ResourceId);
        Assert.Equal("Alice", member.ResourceName);
        Assert.Equal("DEV", member.Role);
        Assert.Equal(100, member.AvailabilityPct);
        Assert.Equal(1.0, member.DailyCapacity);
    }

    [Fact]
    public void Create_NormalizesResourceIdToUppercase()
    {
        var member = TeamMember.Create("dev-001", "Bob", "DEV", "Delivery", 80, 1, DateTime.Today);
        Assert.Equal("DEV-001", member.ResourceId);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("D-1")]
    public void Create_InvalidResourceId_ThrowsDomainException(string id)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create(id, "Alice", "DEV", "Delivery", 100, 1, DateTime.Today));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyResourceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", name, "DEV", "Delivery", 100, 1, DateTime.Today));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_InvalidAvailabilityPct_ThrowsDomainException(double pct)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", pct, 1, DateTime.Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ZeroOrNegativeCapacity_ThrowsDomainException(double cap)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, cap, DateTime.Today));
    }

    [Fact]
    public void Create_TrimsResourceName()
    {
        var member = TeamMember.Create("DEV-001", "  Alice  ", "DEV", "Delivery", 100, 1, DateTime.Today);
        Assert.Equal("Alice", member.ResourceName);
    }

    [Fact]
    public void Create_WithNotes_SetsNotes()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today, active: "YES", notes: "Note here");
        Assert.Equal("Note here", member.Notes);
    }

    [Fact]
    public void Create_BoundaryAvailability_ZeroPercent_DoesNotThrow()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 0, 1, DateTime.Today);
        Assert.Equal(0, member.AvailabilityPct);
    }

    [Fact]
    public void Create_BoundaryAvailability_HundredPercent_DoesNotThrow()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today);
        Assert.Equal(100, member.AvailabilityPct);
    }

    [Fact]
    public void Create_WithNullNotes_SetsNull()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today);
        Assert.Null(member.Notes);
    }

    [Fact]
    public void Create_EndDate_DefaultsToNull()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today);
        Assert.Null(member.EndDate);
    }
}

// ============================================================
// Adjustment.Create — domain factory
// ============================================================

public class AdjustmentDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedAdjustment()
    {
        var start = DateTime.Today;
        var end = DateTime.Today.AddDays(7);
        var adj = Adjustment.Create("DEV-001", "VACATION", 0, start, end, "Summer");

        Assert.Equal("DEV-001", adj.ResourceId);
        Assert.Equal("VACATION", adj.AdjType);
        Assert.Equal(0, adj.AvailabilityPct);
        Assert.Equal(start, adj.AdjStart);
        Assert.Equal(end, adj.AdjEnd);
        Assert.Equal("Summer", adj.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyResourceId_ThrowsDomainException(string id)
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create(id, "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_InvalidAvailabilityPct_ThrowsDomainException(double pct)
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create("DEV-001", "VACATION", pct, DateTime.Today, DateTime.Today.AddDays(3)));
    }

    [Fact]
    public void Create_EndBeforeStart_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create("DEV-001", "VACATION", 0,
                DateTime.Today.AddDays(5), DateTime.Today));
    }

    [Fact]
    public void Create_StartEqualsEnd_DoesNotThrow()
    {
        var today = DateTime.Today;
        var adj = Adjustment.Create("DEV-001", "TRAINING", 50, today, today);
        Assert.Equal(today, adj.AdjStart);
        Assert.Equal(today, adj.AdjEnd);
    }

    [Fact]
    public void Create_TrimsResourceId()
    {
        var adj = Adjustment.Create("  DEV-001  ", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal("DEV-001", adj.ResourceId);
    }

    [Fact]
    public void Create_WithNullNotes_SetsNull()
    {
        var adj = Adjustment.Create("DEV-001", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Null(adj.Notes);
    }

    [Fact]
    public void Create_PreservesResourceIdCase_ButTrims()
    {
        // Adjustment.Create trims but does NOT normalize to uppercase (unlike TeamMember.Create)
        var adj = Adjustment.Create("  dev-001  ", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal("dev-001", adj.ResourceId);
    }
}

// ============================================================
// Holiday.Create — domain factory (date range)
// ============================================================

public class HolidayDomainTests
{
    [Fact]
    public void Create_ValidRange_ReturnsPopulatedHoliday()
    {
        var start = new DateTime(2026, 3, 30);
        var end = new DateTime(2026, 4, 2);
        var holiday = Holiday.Create("Eid Al-Fitr", start, end, "RELIGIOUS", "Festival");

        Assert.Equal("Eid Al-Fitr", holiday.HolidayName);
        Assert.Equal(start.Date, holiday.StartDate);
        Assert.Equal(end.Date, holiday.EndDate);
        Assert.Equal("RELIGIOUS", holiday.HolidayType);
        Assert.Equal("Festival", holiday.Notes);
        Assert.Equal(4, holiday.DurationDays);
    }

    [Fact]
    public void Create_SingleDay_OverloadSetsStartAndEndEqual()
    {
        var date = new DateTime(2026, 9, 23);
        var holiday = Holiday.Create("National Day", date, "NATIONAL", "Key date");

        Assert.Equal("National Day", holiday.HolidayName);
        Assert.Equal(date.Date, holiday.StartDate);
        Assert.Equal(date.Date, holiday.EndDate);
        Assert.Equal(1, holiday.DurationDays);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() =>
            Holiday.Create(name, DateTime.Today, DateTime.Today));
    }

    [Fact]
    public void Create_StartAfterEnd_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Holiday.Create("Bad Holiday",
                new DateTime(2026, 9, 25),
                new DateTime(2026, 9, 23)));
    }

    [Fact]
    public void Create_StripsTimeComponent_BothDates()
    {
        var startWithTime = new DateTime(2026, 9, 23, 14, 30, 0);
        var endWithTime = new DateTime(2026, 9, 25, 8, 0, 0);
        var holiday = Holiday.Create("National Day", startWithTime, endWithTime);

        Assert.Equal(new DateTime(2026, 9, 23), holiday.StartDate);
        Assert.Equal(new DateTime(2026, 9, 25), holiday.EndDate);
    }

    [Fact]
    public void Create_DefaultsToNationalType()
    {
        var holiday = Holiday.Create("Some Holiday", DateTime.Today, DateTime.Today);
        Assert.Equal("NATIONAL", holiday.HolidayType);
    }

    [Fact]
    public void Create_TrimsHolidayName()
    {
        var holiday = Holiday.Create("  Eid  ", DateTime.Today, DateTime.Today);
        Assert.Equal("Eid", holiday.HolidayName);
    }

    [Fact]
    public void Create_StartEqualsEnd_DoesNotThrow()
    {
        var date = new DateTime(2026, 1, 1);
        var holiday = Holiday.Create("New Year", date, date);
        Assert.Equal(date, holiday.StartDate);
        Assert.Equal(date, holiday.EndDate);
        Assert.Equal(1, holiday.DurationDays);
    }

    [Fact]
    public void Create_NullName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Holiday.Create(null!, DateTime.Today, DateTime.Today));
    }

    [Fact]
    public void Create_WithNotes_PersistsNotes()
    {
        var holiday = Holiday.Create("Test Holiday", DateTime.Today, DateTime.Today, "NATIONAL", "Some notes");
        Assert.Equal("Some notes", holiday.Notes);
    }

    [Fact]
    public void Create_WithCustomType_PersistsType()
    {
        var holiday = Holiday.Create("Eid", DateTime.Today, DateTime.Today, "RELIGIOUS");
        Assert.Equal("RELIGIOUS", holiday.HolidayType);
    }

    [Fact]
    public void Create_LargeRange_CalculatesDurationCorrectly()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 30);
        var holiday = Holiday.Create("Long Holiday", start, end);
        Assert.Equal(30, holiday.DurationDays);
    }

    [Fact]
    public void Create_SingleDayOverload_DefaultsToNationalType()
    {
        var holiday = Holiday.Create("Independence Day", new DateTime(2026, 7, 4));
        Assert.Equal("NATIONAL", holiday.HolidayType);
    }

    [Fact]
    public void Create_SingleDayOverload_StripsTime()
    {
        var dateWithTime = new DateTime(2026, 7, 4, 15, 30, 45);
        var holiday = Holiday.Create("Independence Day", dateWithTime);
        Assert.Equal(new DateTime(2026, 7, 4), holiday.StartDate);
        Assert.Equal(new DateTime(2026, 7, 4), holiday.EndDate);
    }

    [Fact]
    public void Create_RangeOverload_NullNotes_SetsNull()
    {
        var holiday = Holiday.Create("Test Holiday", DateTime.Today, DateTime.Today, "NATIONAL", null);
        Assert.Null(holiday.Notes);
    }
}

// ============================================================
// DomainException
// ============================================================

public class DomainExceptionTests
{
    private static List<EffortBreakdownSpec> B(double dev) => TestDatabaseHelper.MakeBreakdown(dev);

    [Fact]
    public void DomainException_HasCorrectMessage()
    {
        var ex = new DomainException("test error");
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void DomainException_IsException()
    {
        var ex = new DomainException("msg");
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void TaskItem_Create_ThrowsDomainException_NotArgumentException()
    {
        // Ensure it's specifically DomainException (not ArgumentException etc.)
        var ex = Assert.Throws<DomainException>(() =>
            TaskItem.Create("BAD", "Service", 5, B(5)));
        Assert.Contains("Task ID", ex.Message);
    }
}

// ============================================================
// DomainConstants — GetWeekendDays
// ============================================================

public class DomainConstantsTests
{
    [Fact]
    public void GetWeekendDays_SunThu_ReturnsFridayAndSaturday()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.SunThu);
        Assert.Contains(DayOfWeek.Friday, days);
        Assert.Contains(DayOfWeek.Saturday, days);
        Assert.Equal(2, days.Count);
    }

    [Fact]
    public void GetWeekendDays_MonFri_ReturnsSaturdayAndSunday()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.MonFri);
        Assert.Contains(DayOfWeek.Saturday, days);
        Assert.Contains(DayOfWeek.Sunday, days);
        Assert.Equal(2, days.Count);
    }

    [Fact]
    public void GetWeekendDays_UnknownCode_DefaultsToSunThu()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays("unknown_code");
        Assert.Contains(DayOfWeek.Friday, days);
        Assert.Contains(DayOfWeek.Saturday, days);
        Assert.Equal(2, days.Count);
    }

    [Fact]
    public void GetWeekendDays_EmptyString_DefaultsToSunThu()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays("");
        Assert.Contains(DayOfWeek.Friday, days);
        Assert.Contains(DayOfWeek.Saturday, days);
    }

    [Fact]
    public void GetWeekendDays_Null_DefaultsToSunThu()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays(null!);
        Assert.Contains(DayOfWeek.Friday, days);
        Assert.Contains(DayOfWeek.Saturday, days);
    }

    [Fact]
    public void GetWeekendDays_MonFri_FridayIsNotWeekend()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.MonFri);
        Assert.DoesNotContain(DayOfWeek.Friday, days);
    }

    [Fact]
    public void GetWeekendDays_SunThu_SundayIsNotWeekend()
    {
        var days = DomainConstants.WorkingWeek.GetWeekendDays(DomainConstants.WorkingWeek.SunThu);
        Assert.DoesNotContain(DayOfWeek.Sunday, days);
    }

    [Fact]
    public void Constants_TaskStatus_MatchExpectedValues()
    {
        Assert.Equal("NOT_STARTED", DomainConstants.TaskStatus.NotStarted);
        Assert.Equal("IN_PROGRESS", DomainConstants.TaskStatus.InProgress);
        Assert.Equal("COMPLETED", DomainConstants.TaskStatus.Completed);
    }

    [Fact]
    public void Constants_DeliveryRisk_MatchExpectedValues()
    {
        Assert.Equal("ON_TRACK", DomainConstants.DeliveryRisk.OnTrack);
        Assert.Equal("AT_RISK", DomainConstants.DeliveryRisk.AtRisk);
        Assert.Equal("LATE", DomainConstants.DeliveryRisk.Late);
    }

    [Fact]
    public void Constants_SettingKeys_MatchExpectedValues()
    {
        Assert.Equal("plan_start_date", DomainConstants.SettingKeys.PlanStartDate);
        Assert.Equal("at_risk_threshold", DomainConstants.SettingKeys.AtRiskThreshold);
        Assert.Equal("working_week", DomainConstants.SettingKeys.WorkingWeek);
    }

    [Fact]
    public void Constants_DefaultTeam_IsDelivery()
    {
        Assert.Equal("Delivery", DomainConstants.DefaultTeam);
    }

    [Fact]
    public void Constants_WorkingWeekCodes_MatchExpectedValues()
    {
        Assert.Equal("SUN_THU", DomainConstants.WorkingWeek.SunThu);
        Assert.Equal("MON_FRI", DomainConstants.WorkingWeek.MonFri);
    }
}

// ============================================================
// TaskItem.Create — additional coverage
// ============================================================

public class TaskItemDomainAdditionalTests
{
    private static List<EffortBreakdownSpec> B(double dev, double qa = 1) => TestDatabaseHelper.MakeBreakdown(dev, qa);

    [Fact]
    public void Create_NullTaskId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TaskItem.Create(null!, "Service", 5, B(5)));
    }

    [Fact]
    public void Create_Factory_SetsDefaultStatus_NotStarted()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Equal(DomainConstants.TaskStatus.NotStarted, task.Status);
    }

    [Fact]
    public void Create_Factory_SetsDefaultDeliveryRisk_OnTrack()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Equal(DomainConstants.DeliveryRisk.OnTrack, task.DeliveryRisk);
    }

    [Fact]
    public void Create_Factory_SetsTimestamps()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        var after = DateTime.Now.AddSeconds(1);

        Assert.True(task.CreatedAt >= before && task.CreatedAt <= after);
        Assert.True(task.UpdatedAt >= before && task.UpdatedAt <= after);
    }

    [Fact]
    public void Create_Factory_SchedulingRank_DefaultsToNull()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Null(task.SchedulingRank);
    }

    [Fact]
    public void Create_Factory_PeakConcurrency_DefaultsToNull()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Null(task.PeakConcurrency);
    }

    [Fact]
    public void Create_Factory_PlannedDates_DefaultToNull()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        Assert.Null(task.PlannedStart);
        Assert.Null(task.PlannedFinish);
        Assert.Null(task.Duration);
    }

    [Fact]
    public void Create_CombinedBoundaries_Priority1_StrictDate_Dependencies()
    {
        var strict = new DateTime(2026, 12, 31);
        var task = TaskItem.Create("SVC-001", "Service", 1, B(0.001), strict);
        task.AddDependency("SVC-002");
        task.AddDependency("SVC-003");

        Assert.Equal(1, task.Priority);
        Assert.Equal(strict, task.StrictDate);
        Assert.Equal("SVC-002,SVC-003", task.DependsOnTaskIds);
        Assert.True(task.TotalEstimationDays > 0);
    }

    [Fact]
    public void AddDependency_AcceptsArbitraryPredecessorIds()
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5));
        task.AddDependency("GARBAGE");
        Assert.Equal("GARBAGE", task.DependsOnTaskIds);
    }

    [Fact]
    public void Create_StrictDateInPast_AcceptedWithoutValidation()
    {
        var pastDate = new DateTime(2020, 1, 1);
        var task = TaskItem.Create("SVC-001", "Service", 5, B(5), pastDate);
        Assert.Equal(pastDate, task.StrictDate);
    }
}

// ============================================================
// TeamMember.Create — additional coverage
// ============================================================

public class TeamMemberDomainAdditionalTests
{
    [Fact]
    public void Create_NullResourceId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create(null!, "Alice", "DEV", "Delivery", 100, 1, DateTime.Today));
    }

    [Fact]
    public void Create_NullResourceName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", null!, "DEV", "Delivery", 100, 1, DateTime.Today));
    }

    [Fact]
    public void Create_DefaultActive_IsYes()
    {
        // active parameter defaults to "YES" when not specified
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today);
        Assert.Equal(DomainConstants.ActiveStatus.Yes, member.Active);
    }

    [Fact]
    public void Create_SetsTeamCorrectly()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Platform", 100, 1, DateTime.Today);
        Assert.Equal("Platform", member.Team);
    }

    [Fact]
    public void Create_SetsStartDateCorrectly()
    {
        var startDate = new DateTime(2026, 3, 15);
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, startDate);
        Assert.Equal(startDate, member.StartDate);
    }

    [Fact]
    public void Create_PreservesRoleExactly()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "QA", "Delivery", 100, 1, DateTime.Today);
        Assert.Equal("QA", member.Role);
    }

    [Fact]
    public void Create_ArbitraryRole_Rejected()
    {
        // Role is validated against DomainConstants.ResourceRole.AllRoles.
        var ex = Assert.Throws<SoftwareDeliveryPlanner.SharedKernel.DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "Arbitrary Role", "Delivery", 100, 1, DateTime.Today));
        Assert.Contains("Invalid role", ex.Message);
    }

    [Fact]
    public void Create_ArbitraryTeam_Accepted()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Any Team", 100, 1, DateTime.Today);
        Assert.Equal("Any Team", member.Team);
    }

    [Fact]
    public void Create_ArbitraryActive_Rejected()
    {
        // Active is validated against DomainConstants.ActiveStatus.{Yes,No}.
        var ex = Assert.Throws<SoftwareDeliveryPlanner.SharedKernel.DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today, active: "Maybe"));
        Assert.Contains("Invalid active status", ex.Message);
    }

    [Fact]
    public void Create_NaN_DailyCapacity_Bypasses_Validation()
    {
        // NaN <= 0 is false, so it passes the > 0 guard
        try
        {
            var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, double.NaN, DateTime.Today);
            Assert.True(double.IsNaN(member.DailyCapacity));
        }
        catch (DomainException)
        {
            // Ideal behavior — NaN rejected
        }
    }

    [Fact]
    public void Create_NaN_AvailabilityPct_Bypasses_Validation()
    {
        // Percentage.TryCreate(NaN) — NaN < 0 is false AND NaN > 100 is false → returns true
        try
        {
            var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", double.NaN, 1, DateTime.Today);
            Assert.True(double.IsNaN(member.AvailabilityPct));
        }
        catch (DomainException)
        {
            // Ideal behavior
        }
    }

    [Fact]
    public void Create_FractionalCapacity_Boundary()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 0.001, DateTime.Today);
        Assert.Equal(0.001, member.DailyCapacity);
    }

    [Fact]
    public void Create_SetsCreatedAt()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var member = TeamMember.Create("DEV-001", "Alice", "DEV", "Delivery", 100, 1, DateTime.Today);
        var after = DateTime.Now.AddSeconds(1);
        Assert.True(member.CreatedAt >= before && member.CreatedAt <= after);
    }
}

// ============================================================
// Adjustment.Create — additional coverage
// ============================================================

public class AdjustmentDomainAdditionalTests
{
    [Fact]
    public void Create_NullResourceId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create(null!, "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3)));
    }

    [Fact]
    public void Create_BoundaryAvailabilityPct_Zero_Accepted()
    {
        var adj = Adjustment.Create("DEV-001", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal(0, adj.AvailabilityPct);
    }

    [Fact]
    public void Create_BoundaryAvailabilityPct_Hundred_Accepted()
    {
        var adj = Adjustment.Create("DEV-001", "TRAINING", 100, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal(100, adj.AvailabilityPct);
    }

    [Fact]
    public void Create_DoesNotValidateResourceIdFormat()
    {
        // Adjustment.Create only checks IsNullOrWhiteSpace, not ResourceIdVO format
        var adj = Adjustment.Create("INVALID", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal("INVALID", adj.ResourceId);
    }

    [Fact]
    public void Create_NullAdjType_Accepted()
    {
        // No validation on adjType — null accepted
        var adj = Adjustment.Create("DEV-001", null!, 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Null(adj.AdjType);
    }

    [Fact]
    public void Create_EmptyAdjType_Accepted()
    {
        var adj = Adjustment.Create("DEV-001", "", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal("", adj.AdjType);
    }

    [Fact]
    public void Create_NaN_AvailabilityPct_Bypasses_Validation()
    {
        try
        {
            var adj = Adjustment.Create("DEV-001", "VACATION", double.NaN, DateTime.Today, DateTime.Today.AddDays(3));
            Assert.True(double.IsNaN(adj.AvailabilityPct));
        }
        catch (DomainException)
        {
            // Ideal behavior
        }
    }

    [Fact]
    public void Create_WhitespaceOnlyNotes_StoredAsIs()
    {
        var adj = Adjustment.Create("DEV-001", "VACATION", 0, DateTime.Today, DateTime.Today.AddDays(3), "   ");
        Assert.Equal("   ", adj.Notes);
    }
}

// ============================================================
// Holiday.Create — additional coverage
// ============================================================

public class HolidayDomainAdditionalTests
{
    [Fact]
    public void Create_SingleDay_EmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Holiday.Create("", DateTime.Today));
    }

    [Fact]
    public void Create_SingleDay_NullName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Holiday.Create(null!, DateTime.Today));
    }

    [Fact]
    public void Create_NoValidation_OnHolidayType()
    {
        // holidayType accepts any value
        var holiday = Holiday.Create("Test", DateTime.Today, DateTime.Today, "CustomType");
        Assert.Equal("CustomType", holiday.HolidayType);
    }

    [Fact]
    public void Create_NullHolidayType_Accepted()
    {
        var holiday = Holiday.Create("Test", DateTime.Today, DateTime.Today, null!);
        Assert.Null(holiday.HolidayType);
    }

    [Fact]
    public void Create_SingleDay_NullNotes_SetsNull()
    {
        var holiday = Holiday.Create("Test", DateTime.Today, notes: null);
        Assert.Null(holiday.Notes);
    }

    [Fact]
    public void Create_DoesNotTrimNotes()
    {
        var holiday = Holiday.Create("Test", DateTime.Today, DateTime.Today, "NATIONAL", "  note  ");
        Assert.Equal("  note  ", holiday.Notes);
    }
}
