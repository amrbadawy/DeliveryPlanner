namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Base record for all domain events. Provides <see cref="OccurredOn"/> timestamp automatically.
/// Domain events must be <c>sealed record</c> types inheriting from this base.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
