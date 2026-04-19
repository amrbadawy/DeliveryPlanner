using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Events;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.Infrastructure.Extensions;
using SoftwareDeliveryPlanner.Infrastructure.Services;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Records audit log calls for verification in tests.
/// </summary>
internal sealed class RecordingAuditService : IAuditService
{
    public List<(string Action, string EntityType, string EntityId, string Description)> Entries { get; } = new();

    public Task LogAsync(string action, string entityType, string entityId, string description, string? oldValue = null, string? newValue = null)
    {
        Entries.Add((action, entityType, entityId, description));
        return Task.CompletedTask;
    }

    public Task<List<AuditEntry>> GetRecentAsync(int count = 50) => Task.FromResult(new List<AuditEntry>());
}

public class DomainEventAuditHandlerTests
{
    private readonly RecordingAuditService _auditService = new();
    private readonly DomainEventAuditHandler _handler;

    public DomainEventAuditHandlerTests()
    {
        _handler = new DomainEventAuditHandler(_auditService);
    }

    [Fact]
    public async Task Handle_TaskCreatedEvent_LogsCreatedAction()
    {
        var domainEvent = new TaskCreatedEvent("SVC-001", "Payment Service");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Created, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Task, entry.EntityType);
        Assert.Equal("SVC-001", entry.EntityId);
        Assert.Contains("Payment Service", entry.Description);
    }

    [Fact]
    public async Task Handle_TaskUpdatedEvent_LogsUpdatedAction()
    {
        var domainEvent = new TaskUpdatedEvent("SVC-002");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Updated, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Task, entry.EntityType);
        Assert.Equal("SVC-002", entry.EntityId);
    }

    [Fact]
    public async Task Handle_ResourceCreatedEvent_LogsCreatedAction()
    {
        var domainEvent = new ResourceCreatedEvent("RES-001", "John Doe");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Created, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Resource, entry.EntityType);
        Assert.Equal("RES-001", entry.EntityId);
        Assert.Contains("John Doe", entry.Description);
    }

    [Fact]
    public async Task Handle_ResourceUpdatedEvent_LogsUpdatedAction()
    {
        var domainEvent = new ResourceUpdatedEvent("RES-002");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Updated, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Resource, entry.EntityType);
        Assert.Equal("RES-002", entry.EntityId);
    }

    [Fact]
    public async Task Handle_HolidayCreatedEvent_LogsCreatedAction()
    {
        var startDate = new DateTime(2026, 1, 1);
        var domainEvent = new HolidayCreatedEvent("New Year", startDate);
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Created, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Holiday, entry.EntityType);
        Assert.Contains("New Year", entry.Description);
        Assert.Contains("2026-01-01", entry.Description);
    }

    [Fact]
    public async Task Handle_HolidayUpdatedEvent_LogsUpdatedAction()
    {
        var domainEvent = new HolidayUpdatedEvent(42);
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Updated, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Holiday, entry.EntityType);
        Assert.Equal("42", entry.EntityId);
    }

    [Fact]
    public async Task Handle_AdjustmentAddedEvent_LogsCreatedAction()
    {
        var domainEvent = new AdjustmentAddedEvent("RES-001", "Leave");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Created, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Adjustment, entry.EntityType);
        Assert.Equal("RES-001", entry.EntityId);
        Assert.Contains("Leave", entry.Description);
    }

    [Fact]
    public async Task Handle_AdjustmentRemovedEvent_LogsDeletedAction()
    {
        var domainEvent = new AdjustmentRemovedEvent("RES-001", 99);
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        var entry = Assert.Single(_auditService.Entries);
        Assert.Equal(DomainConstants.AuditAction.Deleted, entry.Action);
        Assert.Equal(DomainConstants.EntityType.Adjustment, entry.EntityType);
        Assert.Equal("99", entry.EntityId);
        Assert.Contains("RES-001", entry.Description);
    }

    [Fact]
    public async Task Handle_UnknownEvent_DoesNotLog()
    {
        // DataChangedEvent is not handled by the audit handler
        var domainEvent = new DataChangedEvent("Task", "Modified");
        var notification = new DomainEventNotification(domainEvent);

        await _handler.Handle(notification, CancellationToken.None);

        Assert.Empty(_auditService.Entries);
    }
}
