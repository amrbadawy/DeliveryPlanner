using SoftwareDeliveryPlanner.Application.Scenarios.Commands;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="SetScenarioZoomLevelCommandValidator"/>.
/// </summary>
public class SetScenarioZoomLevelCommandValidatorTests
{
    private readonly SetScenarioZoomLevelCommandValidator _validator = new();

    [Fact]
    public void AllowsNullZoomLevel()
    {
        var result = _validator.Validate(new SetScenarioZoomLevelCommand(1, null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AllowsEmptyZoomLevel()
    {
        var result = _validator.Validate(new SetScenarioZoomLevelCommand(1, ""));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("DAY")]
    [InlineData("WEEK")]
    [InlineData("MONTH")]
    [InlineData("QUARTER")]
    public void AcceptsCanonicalZoomLevels(string value)
    {
        var result = _validator.Validate(new SetScenarioZoomLevelCommand(1, value));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsInvalidZoomLevel()
    {
        var result = _validator.Validate(new SetScenarioZoomLevelCommand(1, "BOGUS"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsZeroOrNegativeScenarioId()
    {
        var r1 = _validator.Validate(new SetScenarioZoomLevelCommand(0, "WEEK"));
        var r2 = _validator.Validate(new SetScenarioZoomLevelCommand(-1, "WEEK"));
        Assert.False(r1.IsValid);
        Assert.False(r2.IsValid);
    }
}
