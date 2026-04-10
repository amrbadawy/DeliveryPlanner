using SoftwareDeliveryPlanner.Domain.SharedKernel;
using SoftwareDeliveryPlanner.Models;

namespace SoftwareDeliveryPlanner.Tests;

// ============================================================
// TaskItem.Create — domain factory
// ============================================================

public class TaskItemDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedTaskItem()
    {
        var task = TaskItem.Create("SVC-001", "My Service", 10, 2, 5);

        Assert.Equal("SVC-001", task.TaskId);
        Assert.Equal("My Service", task.ServiceName);
        Assert.Equal(10, task.DevEstimation);
        Assert.Equal(2, task.MaxDev);
        Assert.Equal(5, task.Priority);
        Assert.Null(task.StrictDate);
    }

    [Fact]
    public void Create_NormalizesTaskIdToUppercase()
    {
        var task = TaskItem.Create("svc-001", "Service", 5, 1, 5);
        Assert.Equal("SVC-001", task.TaskId);
    }

    [Fact]
    public void Create_WithStrictDate_SetsStrictDate()
    {
        var date = new DateTime(2026, 12, 31);
        var task = TaskItem.Create("SVC-002", "Service", 5, 1, 5, date);
        Assert.Equal(date, task.StrictDate);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("123-abc")]
    [InlineData("A-1")]
    public void Create_InvalidTaskId_ThrowsDomainException(string invalidId)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create(invalidId, "Service", 5, 1, 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyServiceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", name, 5, 1, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public void Create_ZeroOrNegativeEstimation_ThrowsDomainException(double est)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", "Service", est, 1, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Create_ZeroOrNegativeMaxDev_ThrowsDomainException(double maxDev)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", "Service", 5, maxDev, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Create_InvalidPriority_ThrowsDomainException(int priority)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("SVC-001", "Service", 5, 1, priority));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Create_ValidPriority_DoesNotThrow(int priority)
    {
        var task = TaskItem.Create("SVC-001", "Service", 5, 1, priority);
        Assert.Equal(priority, task.Priority);
    }

    [Fact]
    public void Create_TrimsServiceName()
    {
        var task = TaskItem.Create("SVC-001", "  My Service  ", 5, 1, 5);
        Assert.Equal("My Service", task.ServiceName);
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
            "DEV-001", "Alice", "Developer", "Delivery",
            100, 1.0, DateTime.Today);

        Assert.Equal("DEV-001", member.ResourceId);
        Assert.Equal("Alice", member.ResourceName);
        Assert.Equal("Developer", member.Role);
        Assert.Equal(100, member.AvailabilityPct);
        Assert.Equal(1.0, member.DailyCapacity);
    }

    [Fact]
    public void Create_NormalizesResourceIdToUppercase()
    {
        var member = TeamMember.Create("dev-001", "Bob", "Developer", "Delivery", 80, 1, DateTime.Today);
        Assert.Equal("DEV-001", member.ResourceId);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("D-1")]
    public void Create_InvalidResourceId_ThrowsDomainException(string id)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create(id, "Alice", "Developer", "Delivery", 100, 1, DateTime.Today));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyResourceName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", name, "Developer", "Delivery", 100, 1, DateTime.Today));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_InvalidAvailabilityPct_ThrowsDomainException(double pct)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "Developer", "Delivery", pct, 1, DateTime.Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ZeroOrNegativeCapacity_ThrowsDomainException(double cap)
    {
        Assert.Throws<DomainException>(() =>
            TeamMember.Create("DEV-001", "Alice", "Developer", "Delivery", 100, cap, DateTime.Today));
    }

    [Fact]
    public void Create_TrimsResourceName()
    {
        var member = TeamMember.Create("DEV-001", "  Alice  ", "Developer", "Delivery", 100, 1, DateTime.Today);
        Assert.Equal("Alice", member.ResourceName);
    }

    [Fact]
    public void Create_WithNotes_SetsNotes()
    {
        var member = TeamMember.Create("DEV-001", "Alice", "Developer", "Delivery", 100, 1, DateTime.Today, "Yes", "Note here");
        Assert.Equal("Note here", member.Notes);
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
        var adj = Adjustment.Create("DEV-001", "Vacation", 0, start, end, "Summer");

        Assert.Equal("DEV-001", adj.ResourceId);
        Assert.Equal("Vacation", adj.AdjType);
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
            Adjustment.Create(id, "Vacation", 0, DateTime.Today, DateTime.Today.AddDays(3)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_InvalidAvailabilityPct_ThrowsDomainException(double pct)
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create("DEV-001", "Vacation", pct, DateTime.Today, DateTime.Today.AddDays(3)));
    }

    [Fact]
    public void Create_EndBeforeStart_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Adjustment.Create("DEV-001", "Vacation", 0,
                DateTime.Today.AddDays(5), DateTime.Today));
    }

    [Fact]
    public void Create_StartEqualsEnd_DoesNotThrow()
    {
        var today = DateTime.Today;
        var adj = Adjustment.Create("DEV-001", "Training", 50, today, today);
        Assert.Equal(today, adj.AdjStart);
        Assert.Equal(today, adj.AdjEnd);
    }

    [Fact]
    public void Create_TrimsResourceId()
    {
        var adj = Adjustment.Create("  DEV-001  ", "Vacation", 0, DateTime.Today, DateTime.Today.AddDays(3));
        Assert.Equal("DEV-001", adj.ResourceId);
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
        var holiday = Holiday.Create("Eid Al-Fitr", start, end, "Religious", "Festival");

        Assert.Equal("Eid Al-Fitr", holiday.HolidayName);
        Assert.Equal(start.Date, holiday.StartDate);
        Assert.Equal(end.Date, holiday.EndDate);
        Assert.Equal("Religious", holiday.HolidayType);
        Assert.Equal("Festival", holiday.Notes);
        Assert.Equal(4, holiday.DurationDays);
    }

    [Fact]
    public void Create_SingleDay_OverloadSetsStartAndEndEqual()
    {
        var date = new DateTime(2026, 9, 23);
        var holiday = Holiday.Create("National Day", date, "National", "Key date");

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
        Assert.Equal("National", holiday.HolidayType);
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
}

// ============================================================
// DomainException
// ============================================================

public class DomainExceptionTests
{
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
            TaskItem.Create("BAD", "Service", 5, 1, 5));
        Assert.Contains("Task ID", ex.Message);
    }
}
