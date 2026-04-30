using SoftwareDeliveryPlanner.Domain;
using Xunit;

namespace SoftwareDeliveryPlanner.Tests;

public class GanttZoomLevelTests
{
    [Theory]
    [InlineData("DAY", true)]
    [InlineData("WEEK", true)]
    [InlineData("MONTH", true)]
    [InlineData("QUARTER", true)]
    [InlineData("day", true)]
    [InlineData("Week", true)]
    [InlineData("INVALID", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_acceptsCanonicalAndCaseInsensitive(string? code, bool expected)
    {
        Assert.Equal(expected, DomainConstants.GanttZoomLevel.IsValid(code));
    }

    [Theory]
    [InlineData("DAY", 32.0)]
    [InlineData("MONTH", 4.0)]
    public void PixelsPerDay_returnsExactValuesForExactConstants(string code, double expected)
    {
        Assert.Equal(expected, DomainConstants.GanttZoomLevel.PixelsPerDay(code), 3);
    }

    [Fact]
    public void PixelsPerDay_descendsAsZoomCoarsens()
    {
        var day = DomainConstants.GanttZoomLevel.PixelsPerDay(DomainConstants.GanttZoomLevel.Day);
        var week = DomainConstants.GanttZoomLevel.PixelsPerDay(DomainConstants.GanttZoomLevel.Week);
        var month = DomainConstants.GanttZoomLevel.PixelsPerDay(DomainConstants.GanttZoomLevel.Month);
        var quarter = DomainConstants.GanttZoomLevel.PixelsPerDay(DomainConstants.GanttZoomLevel.Quarter);

        Assert.True(day > week);
        Assert.True(week > month);
        Assert.True(month > quarter);
    }

    [Fact]
    public void PixelsPerDay_unknownCodeReturnsWeekDefault()
    {
        var weekPx = DomainConstants.GanttZoomLevel.PixelsPerDay(DomainConstants.GanttZoomLevel.Week);
        Assert.Equal(weekPx, DomainConstants.GanttZoomLevel.PixelsPerDay("UNKNOWN"), 3);
    }

    [Theory]
    [InlineData(7, "DAY")]
    [InlineData(21, "DAY")]
    [InlineData(22, "WEEK")]
    [InlineData(180, "WEEK")]
    [InlineData(181, "MONTH")]
    [InlineData(540, "MONTH")]
    [InlineData(541, "QUARTER")]
    [InlineData(1500, "QUARTER")]
    public void AdaptiveDefault_picksAppropriateZoomForPlanLength(int totalDays, string expected)
    {
        Assert.Equal(expected, DomainConstants.GanttZoomLevel.AdaptiveDefault(totalDays));
    }

    [Fact]
    public void Levels_containsAllFourInPipelineOrder()
    {
        Assert.Equal(
            new[] { "DAY", "WEEK", "MONTH", "QUARTER" },
            DomainConstants.GanttZoomLevel.Levels.ToArray());
    }

    [Fact]
    public void DisplayName_returnsHumanFriendlyLabels()
    {
        Assert.Equal("Day", DomainConstants.GanttZoomLevel.DisplayName(DomainConstants.GanttZoomLevel.Day));
        Assert.Equal("Week", DomainConstants.GanttZoomLevel.DisplayName(DomainConstants.GanttZoomLevel.Week));
        Assert.Equal("Month", DomainConstants.GanttZoomLevel.DisplayName(DomainConstants.GanttZoomLevel.Month));
        Assert.Equal("Quarter", DomainConstants.GanttZoomLevel.DisplayName(DomainConstants.GanttZoomLevel.Quarter));
    }

    [Fact]
    public void Default_isWeek()
    {
        Assert.Equal("WEEK", DomainConstants.GanttZoomLevel.Default);
    }

    [Fact]
    public void SettingKey_isExpectedSnakeCase()
    {
        Assert.Equal("gantt_zoom_level", DomainConstants.SettingKeys.GanttZoomLevel);
    }
}
