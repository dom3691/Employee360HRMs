namespace Employee360.Domain.Interfaces;

/// <summary>
/// Abstraction over the system clock so business logic (leave day calculations,
/// payroll cut-offs, SLA escalations) is deterministic and testable.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Current UTC instant.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current instant in West Africa Time (UTC+1), the business timezone.</summary>
    DateTimeOffset WatNow { get; }

    /// <summary>Current business date in West Africa Time.</summary>
    DateOnly TodayWat { get; }
}
