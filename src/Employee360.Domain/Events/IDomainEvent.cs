namespace Employee360.Domain.Events;

/// <summary>
/// Marker contract for domain events raised by entities (e.g. LeaveApprovedEvent,
/// EmployeeTerminatedEvent). Events are collected on <see cref="Common.BaseEntity"/>
/// and dispatched by the persistence layer after a successful save, keeping the
/// Domain project free of messaging dependencies.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredOnUtc { get; }
}
