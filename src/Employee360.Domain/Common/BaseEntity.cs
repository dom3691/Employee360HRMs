using Employee360.Domain.Events;

namespace Employee360.Domain.Common;

/// <summary>
/// Root base class for all domain entities. Provides a <see cref="Guid"/> identity
/// and domain-event collection semantics (events are dispatched and cleared by the
/// persistence layer after a successful save).
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Domain events raised by this entity and not yet dispatched.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Queues a domain event for dispatch after the entity is persisted.</summary>
    /// <param name="domainEvent">The event to raise.</param>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Removes a queued domain event.</summary>
    /// <param name="domainEvent">The event to remove.</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    /// <summary>Clears all queued domain events (called by the dispatcher after publishing).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
