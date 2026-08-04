using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Work shift definition (PRD FR-ATT-002): start/end times, grace period, and break.
/// Assigned to departments via <see cref="Department.ShiftId"/>.
/// </summary>
public class Shift : AuditableEntity, ISoftDelete
{
    /// <summary>Display name, e.g. "Day Shift".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Scheduled start time (local WAT).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Scheduled end time (local WAT).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Minutes after start time still counted as on-time (FR-ATT-003).</summary>
    public int GraceMinutes { get; set; }

    /// <summary>Deducted break duration in minutes.</summary>
    public int BreakMinutes { get; set; }

    /// <summary>Departments using this shift.</summary>
    public ICollection<Department> Departments { get; set; } = new List<Department>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
