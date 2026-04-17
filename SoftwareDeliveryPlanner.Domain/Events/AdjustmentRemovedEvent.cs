using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when an <see cref="Models.Adjustment"/> is removed from a <see cref="Models.TeamMember"/>.</summary>
public sealed record AdjustmentRemovedEvent(string ResourceId, int AdjustmentId) : DomainEvent;
