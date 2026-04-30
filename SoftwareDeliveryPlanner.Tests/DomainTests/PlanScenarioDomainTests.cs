using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests;

public class PlanScenarioDomainTests
{
    private static readonly DateTime TestTimestamp = new(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedScenario()
    {
        var finish = new DateTime(2026, 6, 30);
        var scenario = PlanScenario.Create("Sprint 1 Baseline", 10, 5, 3, 2, 0, null, finish, 120.5, "Initial plan", TestTimestamp);

        Assert.Equal("Sprint 1 Baseline", scenario.ScenarioName);
        Assert.Equal(10, scenario.TotalTasks);
        Assert.Equal(5, scenario.OnTrackCount);
        Assert.Equal(3, scenario.AtRiskCount);
        Assert.Equal(2, scenario.LateCount);
        Assert.Equal(0, scenario.UnscheduledCount);
        Assert.Null(scenario.EarliestStart);
        Assert.Equal(finish, scenario.LatestFinish);
        Assert.Equal(120.5, scenario.TotalEstimation);
        Assert.Equal("Initial plan", scenario.Notes);
        Assert.Equal(TestTimestamp, scenario.CreatedAt);
    }

    [Fact]
    public void Create_SetsCreatedAtToProvidedTimestamp()
    {
        var scenario = PlanScenario.Create("Test", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        Assert.Equal(TestTimestamp, scenario.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() => PlanScenario.Create(name, 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp));
    }

    [Fact]
    public void Create_NullName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => PlanScenario.Create(null!, 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp));
    }

    [Fact]
    public void Create_TrimsName()
    {
        var scenario = PlanScenario.Create("  Trimmed  ", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        Assert.Equal("Trimmed", scenario.ScenarioName);
    }

    [Fact]
    public void Create_NonZeroUnscheduledCount_IsStored()
    {
        var scenario = PlanScenario.Create("With Unscheduled", 10, 3, 2, 1, 4, null, null, 50.0, null, TestTimestamp);

        Assert.Equal(4, scenario.UnscheduledCount);
        Assert.Equal(10, scenario.TotalTasks);
        Assert.Equal(3, scenario.OnTrackCount);
        Assert.Equal(2, scenario.AtRiskCount);
        Assert.Equal(1, scenario.LateCount);
    }

    [Fact]
    public void Create_AllUnscheduled_IsValid()
    {
        var scenario = PlanScenario.Create("All Unscheduled", 5, 0, 0, 0, 5, null, null, 25.0, null, TestTimestamp);

        Assert.Equal(5, scenario.UnscheduledCount);
        Assert.Equal(0, scenario.OnTrackCount);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 0)]
    [InlineData(5, -1, 0, 0, 0)]
    [InlineData(5, 0, -1, 0, 0)]
    [InlineData(5, 0, 0, -1, 0)]
    [InlineData(5, 0, 0, 0, -1)]
    public void Create_NegativeCount_ThrowsDomainException(int total, int onTrack, int atRisk, int late, int unscheduled)
    {
        var ex = Assert.Throws<DomainException>(() =>
            PlanScenario.Create("Test", total, onTrack, atRisk, late, unscheduled, null, null, 0, null, TestTimestamp));

        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(5, 3, 2, 1, 1)]   // sum = 7 > 5
    [InlineData(10, 5, 3, 2, 1)]  // sum = 11 > 10
    [InlineData(5, 3, 2, 1, 0)]   // sum = 6 > 5
    [InlineData(5, 0, 0, 0, 0)]   // sum = 0 < 5
    public void Create_SumMismatch_ThrowsDomainException(int total, int onTrack, int atRisk, int late, int unscheduled)
    {
        var ex = Assert.Throws<DomainException>(() =>
            PlanScenario.Create("Test", total, onTrack, atRisk, late, unscheduled, null, null, 0, null, TestTimestamp));

        Assert.Contains("must equal total tasks", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(10, 5, 3, 2, 0)]
    [InlineData(10, 3, 2, 1, 4)]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1)]
    public void Create_ValidSums_Succeeds(int total, int onTrack, int atRisk, int late, int unscheduled)
    {
        var scenario = PlanScenario.Create("Valid", total, onTrack, atRisk, late, unscheduled, null, null, 0, null, TestTimestamp);

        Assert.Equal(total, scenario.TotalTasks);
        Assert.Equal(onTrack + atRisk + late + unscheduled, scenario.TotalTasks);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-100)]
    public void Create_NegativeTotalEstimation_ThrowsDomainException(double estimation)
    {
        var ex = Assert.Throws<DomainException>(() =>
            PlanScenario.Create("Test", 0, 0, 0, 0, 0, null, null, estimation, null, TestTimestamp));

        Assert.Contains("estimation", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ZeroTotalEstimation_Succeeds()
    {
        var scenario = PlanScenario.Create("Test", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        Assert.Equal(0, scenario.TotalEstimation);
    }

    [Fact]
    public void Create_InvertedDateRange_ThrowsDomainException()
    {
        var start = new DateTime(2026, 7, 1);
        var finish = new DateTime(2026, 6, 1); // before start

        var ex = Assert.Throws<DomainException>(() =>
            PlanScenario.Create("Test", 5, 5, 0, 0, 0, start, finish, 10, null, TestTimestamp));

        Assert.Contains("earliest start", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, false)] // both null
    [InlineData(false, true)]  // only finish
    [InlineData(true, false)]  // only start
    [InlineData(true, true)]   // both present, start <= finish
    public void Create_ValidDateCombinations_Succeeds(bool hasStart, bool hasFinish)
    {
        var start = hasStart ? new DateTime(2026, 5, 1) : (DateTime?)null;
        var finish = hasFinish ? new DateTime(2026, 6, 30) : (DateTime?)null;

        var scenario = PlanScenario.Create("Test", 3, 3, 0, 0, 0, start, finish, 10, null, TestTimestamp);

        Assert.Equal(start, scenario.EarliestStart);
        Assert.Equal(finish, scenario.LatestFinish);
    }

    [Fact]
    public void Create_SameDayStartAndFinish_Succeeds()
    {
        var date = new DateTime(2026, 6, 15);
        var scenario = PlanScenario.Create("Same Day", 1, 1, 0, 0, 0, date, date, 1, null, TestTimestamp);

        Assert.Equal(date, scenario.EarliestStart);
        Assert.Equal(date, scenario.LatestFinish);
    }

    [Fact]
    public void AddTaskSnapshot_AddsToCollection()
    {
        var scenario = PlanScenario.Create("Test", 1, 1, 0, 0, 0, null, null, 5.0, null, TestTimestamp);
        var snapshot = ScenarioTaskSnapshot.Create(
            0, "TSK-001", "Service", 5, null, null, null, null, null, null, null, "NOT_STARTED", "ON_TRACK", null, null);

        scenario.AddTaskSnapshot(snapshot);

        Assert.Single(scenario.TaskSnapshots);
        Assert.Equal("TSK-001", scenario.TaskSnapshots.First().TaskId);
    }

    [Fact]
    public void AddTaskSnapshot_NullSnapshot_ThrowsDomainException()
    {
        var scenario = PlanScenario.Create("Test", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);

        Assert.Throws<DomainException>(() => scenario.AddTaskSnapshot(null!));
    }

    [Fact]
    public void AddTaskSnapshot_MultipleSnapshots_AllAdded()
    {
        var scenario = PlanScenario.Create("Multi", 2, 2, 0, 0, 0, null, null, 10.0, null, TestTimestamp);

        scenario.AddTaskSnapshot(ScenarioTaskSnapshot.Create(
            0, "TSK-001", "Service A", 3, null, null, null, null, null, null, null, "NOT_STARTED", "ON_TRACK", null, null));
        scenario.AddTaskSnapshot(ScenarioTaskSnapshot.Create(
            0, "TSK-002", "Service B", 5, null, null, null, null, null, null, null, "NOT_STARTED", "ON_TRACK", null, null));

        Assert.Equal(2, scenario.TaskSnapshots.Count);
    }

    [Fact]
    public void SetGanttZoomLevel_ValidValue_IsStoredUppercase()
    {
        var scenario = PlanScenario.Create("Zoom", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);

        scenario.SetGanttZoomLevel("month");

        Assert.Equal(DomainConstants.GanttZoomLevel.Month, scenario.GanttZoomLevel);
    }

    [Fact]
    public void SetGanttZoomLevel_Null_ClearsOverride()
    {
        var scenario = PlanScenario.Create("Zoom", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);
        scenario.SetGanttZoomLevel(DomainConstants.GanttZoomLevel.Week);

        scenario.SetGanttZoomLevel(null);

        Assert.Null(scenario.GanttZoomLevel);
    }

    [Fact]
    public void SetGanttZoomLevel_Invalid_ThrowsDomainException()
    {
        var scenario = PlanScenario.Create("Zoom", 0, 0, 0, 0, 0, null, null, 0, null, TestTimestamp);

        Assert.Throws<DomainException>(() => scenario.SetGanttZoomLevel("INVALID"));
    }
}
