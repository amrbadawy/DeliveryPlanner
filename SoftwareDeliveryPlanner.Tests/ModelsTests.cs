using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests;

public class ModelsTests
{
    #region TaskItem Tests

    [Fact]
    public void TaskItem_DefaultValues_AreCorrect()
    {
        var task = new TaskItem();
        
        Assert.Equal(string.Empty, task.TaskId);
        Assert.Equal(string.Empty, task.ServiceName);
        Assert.Equal(0, task.DevEstimation);
        Assert.Equal(1.0, task.MaxDev);
        Assert.Equal(5, task.Priority);
        Assert.Equal("Not Started", task.Status);
        Assert.Equal("On Track", task.DeliveryRisk);
        Assert.Null(task.StrictDate);
        Assert.Null(task.PlannedStart);
        Assert.Null(task.PlannedFinish);
    }

    [Fact]
    public void TaskItem_CanSetProperties()
    {
        var task = new TaskItem
        {
            TaskId = "T-001",
            ServiceName = "API Development",
            DevEstimation = 10,
            MaxDev = 2,
            Priority = 1,
            StrictDate = new DateTime(2026, 6, 1),
            Status = "In Progress",
            DeliveryRisk = "At Risk"
        };

        Assert.Equal("T-001", task.TaskId);
        Assert.Equal("API Development", task.ServiceName);
        Assert.Equal(10, task.DevEstimation);
        Assert.Equal(2, task.MaxDev);
        Assert.Equal(1, task.Priority);
        Assert.Equal(new DateTime(2026, 6, 1), task.StrictDate);
        Assert.Equal("In Progress", task.Status);
        Assert.Equal("At Risk", task.DeliveryRisk);
    }

    [Fact]
    public void TaskItem_Timestamps_AreSetOnCreation()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var task = new TaskItem();
        var after = DateTime.Now.AddSeconds(1);

        Assert.True(task.CreatedAt >= before && task.CreatedAt <= after);
        Assert.True(task.UpdatedAt >= before && task.UpdatedAt <= after);
    }

    #endregion

    #region TeamMember Tests

    [Fact]
    public void TeamMember_DefaultValues_AreCorrect()
    {
        var member = new TeamMember();
        
        Assert.Equal(string.Empty, member.ResourceId);
        Assert.Equal(string.Empty, member.ResourceName);
        Assert.Equal("Developer", member.Role);
        Assert.Equal("Delivery", member.Team);
        Assert.Equal(100.0, member.AvailabilityPct);
        Assert.Equal(1.0, member.DailyCapacity);
        Assert.Equal("Yes", member.Active);
    }

    [Fact]
    public void TeamMember_CanSetProperties()
    {
        var member = new TeamMember
        {
            ResourceId = "DEV-001",
            ResourceName = "Ahmed Al-Rashid",
            Role = "Senior Developer",
            Team = "Platform",
            AvailabilityPct = 80.0,
            DailyCapacity = 0.8,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Active = "No"
        };

        Assert.Equal("DEV-001", member.ResourceId);
        Assert.Equal("Ahmed Al-Rashid", member.ResourceName);
        Assert.Equal("Senior Developer", member.Role);
        Assert.Equal("Platform", member.Team);
        Assert.Equal(80.0, member.AvailabilityPct);
        Assert.Equal(0.8, member.DailyCapacity);
        Assert.Equal(new DateTime(2026, 1, 1), member.StartDate);
        Assert.Equal(new DateTime(2026, 12, 31), member.EndDate);
        Assert.Equal("No", member.Active);
    }

    [Fact]
    public void TeamMember_EndDate_IsNullable()
    {
        var member = new TeamMember { EndDate = null };
        Assert.Null(member.EndDate);
    }

    #endregion

    #region Adjustment Tests

    [Fact]
    public void Adjustment_DefaultValues_AreCorrect()
    {
        var adj = new Adjustment();
        
        Assert.Equal(string.Empty, adj.ResourceId);
        Assert.Equal("Other", adj.AdjType);
        Assert.Equal(0.0, adj.AvailabilityPct);
    }

    [Fact]
    public void Adjustment_CanSetProperties()
    {
        var adj = new Adjustment
        {
            ResourceId = "DEV-001",
            AdjType = "Vacation",
            AdjStart = new DateTime(2026, 7, 1),
            AdjEnd = new DateTime(2026, 7, 14),
            AvailabilityPct = 0.0,
            Notes = "Summer vacation"
        };

        Assert.Equal("DEV-001", adj.ResourceId);
        Assert.Equal("Vacation", adj.AdjType);
        Assert.Equal(new DateTime(2026, 7, 1), adj.AdjStart);
        Assert.Equal(new DateTime(2026, 7, 14), adj.AdjEnd);
        Assert.Equal(0.0, adj.AvailabilityPct);
        Assert.Equal("Summer vacation", adj.Notes);
    }

    #endregion

    #region Holiday Tests

    [Fact]
    public void Holiday_DefaultValues_AreCorrect()
    {
        var holiday = new Holiday();
        
        Assert.Equal(string.Empty, holiday.HolidayName);
        Assert.Equal("National", holiday.HolidayType);
    }

    [Fact]
    public void Holiday_CanSetProperties_DateRange()
    {
        var holiday = new Holiday
        {
            HolidayName = "Eid Al-Fitr",
            StartDate = new DateTime(2026, 3, 30),
            EndDate = new DateTime(2026, 4, 2),
            HolidayType = "Religious",
            Notes = "Festival of Breaking Fast"
        };

        Assert.Equal("Eid Al-Fitr", holiday.HolidayName);
        Assert.Equal(new DateTime(2026, 3, 30), holiday.StartDate);
        Assert.Equal(new DateTime(2026, 4, 2), holiday.EndDate);
        Assert.Equal("Religious", holiday.HolidayType);
        Assert.Equal("Festival of Breaking Fast", holiday.Notes);
    }

    [Fact]
    public void Holiday_DurationDays_SingleDay()
    {
        var holiday = new Holiday
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        Assert.Equal(1, holiday.DurationDays);
    }

    [Fact]
    public void Holiday_DurationDays_MultiDay()
    {
        var holiday = new Holiday
        {
            StartDate = new DateTime(2026, 3, 30),
            EndDate = new DateTime(2026, 4, 2)
        };
        Assert.Equal(4, holiday.DurationDays);
    }

    #endregion

    #region CalendarDay Tests

    [Fact]
    public void CalendarDay_DefaultValues_AreCorrect()
    {
        var cal = new CalendarDay();
        
        Assert.Null(cal.DayName);
        Assert.False(cal.IsWorkingDay);
        Assert.False(cal.IsHoliday);
        Assert.Equal(0, cal.BaseCapacity);
        Assert.Equal(0, cal.AdjCapacity);
        Assert.Equal(0, cal.EffectiveCapacity);
        Assert.Equal(0, cal.ReservedCapacity);
        Assert.Equal(0, cal.RemainingCapacity);
        Assert.Null(cal.HolidayName);
        Assert.Equal(default(DateTime), cal.CalendarDate);
        Assert.Equal(0, cal.DateKey);
    }

    [Fact]
    public void CalendarDay_CalculatesRemainingCapacity()
    {
        var cal = new CalendarDay
        {
            EffectiveCapacity = 5.0,
            ReservedCapacity = 2.0
        };
        cal.RemainingCapacity = cal.EffectiveCapacity - cal.ReservedCapacity;

        Assert.Equal(3.0, cal.RemainingCapacity);
    }

    #endregion

    #region Allocation Tests

    [Fact]
    public void Allocation_DefaultValues_AreCorrect()
    {
        var alloc = new Allocation();
        
        Assert.Equal(string.Empty, alloc.AllocationId);
        Assert.Equal(string.Empty, alloc.TaskId);
        Assert.Equal(0, alloc.AssignedDev);
        Assert.Equal(0, alloc.CumulativeEffort);
        Assert.False(alloc.IsComplete);
        Assert.Equal("Not Started", alloc.ServiceStatus);
        Assert.Null(alloc.SchedRank);
        Assert.Null(alloc.MaxDev);
        Assert.Null(alloc.AvailableCapacity);
        Assert.Null(alloc.Task);
    }

    [Fact]
    public void Allocation_CanSetProperties()
    {
        var alloc = new Allocation
        {
            AllocationId = "ALLOC-000001",
            TaskId = "T-001",
            DateKey = 1,
            CalendarDate = new DateTime(2026, 5, 4),
            SchedRank = 1000000,
            MaxDev = 2.0,
            AvailableCapacity = 4.0,
            AssignedDev = 1.0,
            CumulativeEffort = 1.0,
            IsComplete = false,
            ServiceStatus = "In Progress"
        };

        Assert.Equal("ALLOC-000001", alloc.AllocationId);
        Assert.Equal("T-001", alloc.TaskId);
        Assert.Equal(1, alloc.DateKey);
        Assert.Equal(new DateTime(2026, 5, 4), alloc.CalendarDate);
        Assert.Equal(1000000, alloc.SchedRank);
        Assert.Equal(2.0, alloc.MaxDev);
        Assert.Equal(4.0, alloc.AvailableCapacity);
        Assert.Equal(1.0, alloc.AssignedDev);
        Assert.Equal(1.0, alloc.CumulativeEffort);
        Assert.False(alloc.IsComplete);
        Assert.Equal("In Progress", alloc.ServiceStatus);
    }

    #endregion

    #region Setting Tests

    [Fact]
    public void Setting_DefaultValues_AreCorrect()
    {
        var setting = new Setting();
        
        Assert.Equal(string.Empty, setting.Key);
        Assert.Equal(string.Empty, setting.Value);
    }

    [Fact]
    public void Setting_CanSetProperties()
    {
        var setting = new Setting
        {
            Key = "at_risk_threshold",
            Value = "5"
        };

        Assert.Equal("at_risk_threshold", setting.Key);
        Assert.Equal("5", setting.Value);
    }

    #endregion
}
