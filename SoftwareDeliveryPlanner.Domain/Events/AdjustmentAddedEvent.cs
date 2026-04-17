using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>Raised when an <see cref="Models.Adjustment"/> is added to a <see cref="Models.TeamMember"/>.</summary>
public sealed record AdjustmentAddedEvent(string ResourceId, string AdjType) : DomainEvent;
