using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests;

public class PlanScenarioDomainTests
{
    private static readonly DateTime TestTimestamp = new(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedScenario()
    {
        var finish = new DateTime(2026, 6, 30);
        var scenario = PlanScenario.Create("Sprint 1 Baseline", 10, 5, 3, 2, null, finish, 120.5, "Initial plan", TestTimestamp);

        Assert.Equal("Sprint 1 Baseline", scenario.ScenarioName);
        Assert.Equal(10, scenario.TotalTasks);
        Assert.Equal(5, scenario.OnTrackCount);
        Assert.Equal(3, scenario.AtRiskCount);
        Assert.Equal(2, scenario.LateCount);
        Assert.Null(scenario.EarliestStart);
        Assert.Equal(finish, scenario.LatestFinish);
        Assert.Equal(120.5, scenario.TotalEstimation);
        Assert.Equal("Initial plan", scenario.Notes);
        Assert.Equal(TestTimestamp, scenario.CreatedAt);
    }

    [Fact]
    public void Create_SetsCreatedAtToProvidedTimestamp()
    {
        var scenario = PlanScenario.Create("Test", 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        Assert.Equal(TestTimestamp, scenario.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => PlanScenario.Create(name, 0, 0, 0, 0, null, null, 0, null, TestTimestamp));
    }

    [Fact]
    public void Create_NullName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => PlanScenario.Create(null!, 0, 0, 0, 0, null, null, 0, null, TestTimestamp));
    }

    [Fact]
    public void Create_TrimsName()
    {
        var scenario = PlanScenario.Create("  Trimmed  ", 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        Assert.Equal("Trimmed", scenario.ScenarioName);
    }
}
