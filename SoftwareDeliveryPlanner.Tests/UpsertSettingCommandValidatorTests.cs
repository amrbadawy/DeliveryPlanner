using SoftwareDeliveryPlanner.Application.Settings.Commands;
using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="UpsertSettingCommandValidator"/> — validates the centralised
/// enum-style setting checks for <c>gantt_zoom_level</c>, <c>scenario_gantt_zoom_level</c>,
/// and <c>week_numbering</c>.
/// </summary>
public class UpsertSettingCommandValidatorTests
{
    private readonly UpsertSettingCommandValidator _validator = new();

    [Fact]
    public void RejectsUnknownSettingKey()
    {
        var result = _validator.Validate(new UpsertSettingCommand("not_a_real_key", "WEEK"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Key");
    }

    [Theory]
    [InlineData("DAY")]
    [InlineData("WEEK")]
    [InlineData("MONTH")]
    [InlineData("QUARTER")]
    [InlineData("week")] // case-insensitive
    public void AcceptsCanonicalGanttZoomLevels(string value)
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.GanttZoomLevel, value));
        Assert.True(result.IsValid, $"Expected '{value}' to be valid; got: " + string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("BOGUS")]
    [InlineData("YEAR")]
    [InlineData("123")]
    public void RejectsInvalidGanttZoomLevels(string value)
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.GanttZoomLevel, value));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public void RejectsInvalidScenarioGanttZoomLevel()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.ScenarioGanttZoomLevel, "BOGUS"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptsValidScenarioGanttZoomLevel()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.ScenarioGanttZoomLevel, "MONTH"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AllowsClearingGanttZoomLevelWithEmptyValue()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.GanttZoomLevel, ""));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AllowsClearingGanttZoomLevelWithNullValue()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.GanttZoomLevel, null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsInvalidWeekNumberingValue()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.WeekNumbering, "BOGUS"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptsCanonicalWeekNumberingValue()
    {
        var result = _validator.Validate(new UpsertSettingCommand(
            DomainConstants.SettingKeys.WeekNumbering, DomainConstants.WeekNumbering.Iso8601));
        Assert.True(result.IsValid);
    }
}
