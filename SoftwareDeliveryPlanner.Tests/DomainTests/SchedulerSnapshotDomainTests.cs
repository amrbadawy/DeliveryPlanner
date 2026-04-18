using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Tests;

public class SchedulerSnapshotDomainTests
{
    [Fact]
    public void Create_ValidValues_SetsAllProperties()
    {
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var snapshot = SchedulerSnapshot.Create(timestamp, onTrack: 5, atRisk: 3, late: 2, total: 10);

        Assert.Equal(timestamp, snapshot.RunTimestamp);
        Assert.Equal(5, snapshot.OnTrackCount);
        Assert.Equal(3, snapshot.AtRiskCount);
        Assert.Equal(2, snapshot.LateCount);
        Assert.Equal(10, snapshot.TotalTasks);
    }

    [Fact]
    public void Create_ZeroCounts_Works()
    {
        var timestamp = DateTime.UtcNow;

        var snapshot = SchedulerSnapshot.Create(timestamp, onTrack: 0, atRisk: 0, late: 0, total: 0);

        Assert.Equal(0, snapshot.OnTrackCount);
        Assert.Equal(0, snapshot.AtRiskCount);
        Assert.Equal(0, snapshot.LateCount);
        Assert.Equal(0, snapshot.TotalTasks);
    }

    [Fact]
    public void Create_StoresExactTimestamp()
    {
        var timestamp = new DateTime(2025, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

        var snapshot = SchedulerSnapshot.Create(timestamp, onTrack: 1, atRisk: 2, late: 3, total: 6);

        Assert.Equal(timestamp, snapshot.RunTimestamp);
    }
}
