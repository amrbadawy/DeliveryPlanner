using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests.DomainTests;

public class RiskNotificationDomainTests
{
    [Fact]
    public void Create_ValidValues_SetsAllProperties()
    {
        var notification = RiskNotification.Create("TSK-001", "Auth Service", "On Track", "At Risk");

        Assert.Equal("TSK-001", notification.TaskId);
        Assert.Equal("Auth Service", notification.ServiceName);
        Assert.Equal("On Track", notification.PreviousRisk);
        Assert.Equal("At Risk", notification.CurrentRisk);
        Assert.False(notification.IsRead);
        Assert.True(notification.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_EmptyTaskId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            RiskNotification.Create("", "Service", "On Track", "Late"));
    }

    [Fact]
    public void Create_WhitespaceTaskId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            RiskNotification.Create("   ", "Service", "On Track", "Late"));
    }

    [Fact]
    public void Create_IsReadDefaultsToFalse()
    {
        var notification = RiskNotification.Create("TSK-002", "Service", "On Track", "Late");

        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadToTrue()
    {
        var notification = RiskNotification.Create("TSK-003", "Service", "At Risk", "Late");

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
    }
}
