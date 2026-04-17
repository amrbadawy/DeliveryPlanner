using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when a new <see cref="Models.TeamMember"/> is created.</summary>
public sealed record ResourceCreatedEvent(string ResourceId, string ResourceName) : DomainEvent;
