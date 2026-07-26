namespace Employee360.Domain.Events;

/// <summary>
/// Convenience base record for domain events; stamps <see cref="OccurredOnUtc"/>
/// at construction time.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
