namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredOn { get; }
}
