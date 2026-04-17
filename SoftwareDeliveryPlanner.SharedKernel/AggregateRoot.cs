namespace SoftwareDeliveryPlanner.SharedKernel;

/// <summary>
/// Base class for aggregate root entities. Provides domain event collection
/// that can be dispatched after persistence via <see cref="DomainEvents"/>.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Domain events raised by this aggregate, pending dispatch.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Adds a domain event to be dispatched after the aggregate is persisted.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears all pending domain events. Called after dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
