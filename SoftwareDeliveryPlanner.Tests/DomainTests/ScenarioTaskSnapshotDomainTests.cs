using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests;

public class ScenarioTaskSnapshotDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedSnapshot()
    {
        var start = new DateTime(2026, 5, 1);
        var finish = new DateTime(2026, 5, 15);
        var strict = new DateTime(2026, 5, 20);

        var snapshot = ScenarioTaskSnapshot.Create(
            planScenarioId: 1,
            taskId: "TSK-001",
            serviceName: "Auth Service",
            priority: 3,
            schedulingRank: 1,
            plannedStart: start,
            plannedFinish: finish,
            duration: 10,
            strictDate: strict,
            assignedResourceId: "DEV-001",
            assignedDev: 1.0,
            devEstimation: 8.0,
            maxDev: 2.0,
            status: "InProgress",
            deliveryRisk: "OnTrack",
            dependsOnTaskIds: "TSK-000");

        Assert.Equal(1, snapshot.PlanScenarioId);
        Assert.Equal("TSK-001", snapshot.TaskId);
        Assert.Equal("Auth Service", snapshot.ServiceName);
        Assert.Equal(3, snapshot.Priority);
        Assert.Equal(1, snapshot.SchedulingRank);
        Assert.Equal(start, snapshot.PlannedStart);
        Assert.Equal(finish, snapshot.PlannedFinish);
        Assert.Equal(10, snapshot.Duration);
        Assert.Equal(strict, snapshot.StrictDate);
        Assert.Equal("DEV-001", snapshot.AssignedResourceId);
        Assert.Equal(1.0, snapshot.AssignedDev);
        Assert.Equal(8.0, snapshot.DevEstimation);
        Assert.Equal(2.0, snapshot.MaxDev);
        Assert.Equal("InProgress", snapshot.Status);
        Assert.Equal("OnTrack", snapshot.DeliveryRisk);
        Assert.Equal("TSK-000", snapshot.DependsOnTaskIds);
    }

    [Fact]
    public void Create_WithNullableFieldsNull_SetsNullCorrectly()
    {
        var snapshot = ScenarioTaskSnapshot.Create(
            planScenarioId: 1,
            taskId: "TSK-002",
            serviceName: "Payment Service",
            priority: 5,
            schedulingRank: null,
            plannedStart: null,
            plannedFinish: null,
            duration: null,
            strictDate: null,
            assignedResourceId: null,
            assignedDev: null,
            devEstimation: 5.0,
            maxDev: 1.0,
            status: "NotStarted",
            deliveryRisk: "OnTrack",
            dependsOnTaskIds: null);

        Assert.Null(snapshot.SchedulingRank);
        Assert.Null(snapshot.PlannedStart);
        Assert.Null(snapshot.PlannedFinish);
        Assert.Null(snapshot.Duration);
        Assert.Null(snapshot.StrictDate);
        Assert.Null(snapshot.AssignedResourceId);
        Assert.Null(snapshot.AssignedDev);
        Assert.Null(snapshot.DependsOnTaskIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTaskId_ThrowsDomainException(string taskId)
    {
        Assert.Throws<DomainException>(() => ScenarioTaskSnapshot.Create(
            1, taskId, "Service", 5, null, null, null, null, null, null, null, 5.0, 1.0, "NotStarted", "OnTrack", null));
    }

    [Fact]
    public void Create_NullTaskId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ScenarioTaskSnapshot.Create(
            1, null!, "Service", 5, null, null, null, null, null, null, null, 5.0, 1.0, "NotStarted", "OnTrack", null));
    }

    [Fact]
    public void Create_TrimsTaskIdAndServiceName()
    {
        var snapshot = ScenarioTaskSnapshot.Create(
            1, "  TSK-003  ", "  Trimmed Service  ", 5, null, null, null, null, null, null, null, 5.0, 1.0, "NotStarted", "OnTrack", null);

        Assert.Equal("TSK-003", snapshot.TaskId);
        Assert.Equal("Trimmed Service", snapshot.ServiceName);
    }
}
