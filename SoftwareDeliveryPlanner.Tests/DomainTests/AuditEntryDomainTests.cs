using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Tests;

public class AuditEntryDomainTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPopulatedAuditEntry()
    {
        var entry = AuditEntry.Create("Created", "Task", "SVC-001", "Task created", "old", "new");

        Assert.Equal("Created", entry.Action);
        Assert.Equal("Task", entry.EntityType);
        Assert.Equal("SVC-001", entry.EntityId);
        Assert.Equal("Task created", entry.Description);
        Assert.Equal("old", entry.OldValue);
        Assert.Equal("new", entry.NewValue);
    }

    [Fact]
    public void Create_SetsTimestampToApproximatelyNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var entry = AuditEntry.Create("Created", "Task", "SVC-001", "Test");
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(entry.Timestamp >= before && entry.Timestamp <= after);
    }

    [Fact]
    public void Create_OptionalValuesDefaultToNull()
    {
        var entry = AuditEntry.Create("Created", "Task", "SVC-001", "Test");

        Assert.Null(entry.OldValue);
        Assert.Null(entry.NewValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAction_ThrowsDomainException(string action)
    {
        Assert.Throws<DomainException>(() => AuditEntry.Create(action, "Task", "SVC-001", "Test"));
    }

    [Fact]
    public void Create_NullAction_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => AuditEntry.Create(null!, "Task", "SVC-001", "Test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyEntityType_ThrowsDomainException(string entityType)
    {
        Assert.Throws<DomainException>(() => AuditEntry.Create("Created", entityType, "SVC-001", "Test"));
    }

    [Fact]
    public void Create_NullEntityType_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => AuditEntry.Create("Created", null!, "SVC-001", "Test"));
    }

    [Fact]
    public void Create_TrimsInputs()
    {
        var entry = AuditEntry.Create("  Created  ", "  Task  ", "  SVC-001  ", "  Test  ");

        Assert.Equal("Created", entry.Action);
        Assert.Equal("Task", entry.EntityType);
        Assert.Equal("SVC-001", entry.EntityId);
        Assert.Equal("Test", entry.Description);
    }

    [Fact]
    public void Create_NullEntityId_DefaultsToEmptyString()
    {
        var entry = AuditEntry.Create("Created", "Task", null!, "Test");

        Assert.Equal(string.Empty, entry.EntityId);
    }
}
