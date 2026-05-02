using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Web.Services.Gantt;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="WeekHeaderBuilder"/> — produces week-column and month-column
/// models for the Gantt timeline header. Validates working-week start day, week numbering,
/// tooltip year inclusion, and edge cases.
/// </summary>
public class WeekHeaderBuilderTests
{
    [Fact]
    public void BuildWeeks_EmptyRange_ReturnsEmpty()
    {
        var d = new DateTime(2026, 1, 1);
        var weeks = WeekHeaderBuilder.BuildWeeks(d, d, totalDays: 0,
            DomainConstants.WorkingWeek.SunThu, DomainConstants.WeekNumbering.Iso8601);
        Assert.Empty(weeks);
    }

    [Fact]
    public void BuildWeeks_SunThuWorkingWeek_FirstColumnStartsOnSunday()
    {
        // Plan starts Wed 2024-01-03; first Sunday on/after is 2024-01-07.
        var planStart = new DateTime(2024, 1, 3);
        var planEnd = new DateTime(2024, 1, 31);
        var weeks = WeekHeaderBuilder.BuildWeeks(planStart, planEnd, totalDays: 28,
            DomainConstants.WorkingWeek.SunThu, DomainConstants.WeekNumbering.SundayFirstDay);

        Assert.NotEmpty(weeks);
        Assert.Equal(DayOfWeek.Sunday, weeks[0].Start.DayOfWeek);
        Assert.Equal(new DateTime(2024, 1, 7), weeks[0].Start);
    }

    [Fact]
    public void BuildWeeks_MonFriWorkingWeek_FirstColumnStartsOnMonday()
    {
        // Plan starts Wed 2024-01-03; first Monday on/after is 2024-01-08.
        var planStart = new DateTime(2024, 1, 3);
        var planEnd = new DateTime(2024, 1, 31);
        var weeks = WeekHeaderBuilder.BuildWeeks(planStart, planEnd, totalDays: 28,
            DomainConstants.WorkingWeek.MonFri, DomainConstants.WeekNumbering.Iso8601);

        Assert.NotEmpty(weeks);
        Assert.Equal(DayOfWeek.Monday, weeks[0].Start.DayOfWeek);
        Assert.Equal(new DateTime(2024, 1, 8), weeks[0].Start);
    }

    [Fact]
    public void BuildWeeks_FullLabelIncludesYear()
    {
        var planStart = new DateTime(2024, 1, 1);
        var planEnd = new DateTime(2024, 1, 31);
        var weeks = WeekHeaderBuilder.BuildWeeks(planStart, planEnd, totalDays: 30,
            DomainConstants.WorkingWeek.MonFri, DomainConstants.WeekNumbering.Iso8601);

        Assert.NotEmpty(weeks);
        Assert.Contains("2024", weeks[0].FullLabel);
        Assert.StartsWith("Week ", weeks[0].FullLabel);
    }

    [Fact]
    public void BuildWeeks_LabelHasWPrefix()
    {
        var planStart = new DateTime(2024, 1, 1);
        var planEnd = new DateTime(2024, 1, 31);
        var weeks = WeekHeaderBuilder.BuildWeeks(planStart, planEnd, totalDays: 30,
            DomainConstants.WorkingWeek.MonFri, DomainConstants.WeekNumbering.Iso8601);

        Assert.NotEmpty(weeks);
        Assert.StartsWith("W", weeks[0].Label);
    }

    [Fact]
    public void BuildWeeks_EachColumnIsSevenDaysOrTruncated()
    {
        var planStart = new DateTime(2024, 1, 1); // Monday
        var planEnd = new DateTime(2024, 1, 31);
        var weeks = WeekHeaderBuilder.BuildWeeks(planStart, planEnd, totalDays: 30,
            DomainConstants.WorkingWeek.MonFri, DomainConstants.WeekNumbering.Iso8601);

        for (var i = 0; i < weeks.Count - 1; i++)
        {
            // Non-final columns should span 7 days
            Assert.Equal(6, (weeks[i].End - weeks[i].Start).TotalDays);
        }
        // Final column may be truncated at planEnd
        Assert.True(weeks[^1].End <= planEnd);
    }

    [Fact]
    public void BuildMonths_EmptyRange_ReturnsEmpty()
    {
        var d = new DateTime(2026, 1, 1);
        var months = WeekHeaderBuilder.BuildMonths(d, d, totalDays: 0);
        Assert.Empty(months);
    }

    [Fact]
    public void BuildMonths_SpansMultipleMonths_ProducesOneColumnPerMonth()
    {
        var planStart = new DateTime(2024, 1, 15);
        var planEnd = new DateTime(2024, 3, 10);
        var months = WeekHeaderBuilder.BuildMonths(planStart, planEnd, totalDays: 55);

        Assert.Equal(3, months.Count);
        Assert.Contains(months, m => m.Label == "Jan 2024");
        Assert.Contains(months, m => m.Label == "Feb 2024");
        Assert.Contains(months, m => m.Label == "Mar 2024");
    }
}
