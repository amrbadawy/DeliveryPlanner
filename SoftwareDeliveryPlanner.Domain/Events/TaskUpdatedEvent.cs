using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when an existing <see cref="Models.TaskItem"/> is modified.</summary>
public sealed record TaskUpdatedEvent(string TaskId) : DomainEvent;
