using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when an existing <see cref="Models.TeamMember"/> is modified.</summary>
public sealed record ResourceUpdatedEvent(string ResourceId) : DomainEvent;
