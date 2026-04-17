using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when a new <see cref="Models.TaskItem"/> is created.</summary>
public sealed record TaskCreatedEvent(string TaskId, string ServiceName) : DomainEvent;
