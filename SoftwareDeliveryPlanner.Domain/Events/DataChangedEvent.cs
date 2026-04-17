using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Domain.Events;

/// <summary>
/// Generic event indicating data has changed and a reschedule may be needed.
/// Raised as a catch-all when specific entity events are insufficient.
/// </summary>
public sealed record DataChangedEvent(string EntityType, string ChangeType) : DomainEvent;
