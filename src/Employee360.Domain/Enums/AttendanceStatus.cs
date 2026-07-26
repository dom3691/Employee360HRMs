namespace Employee360.Domain.Enums;

/// <summary>
/// Daily attendance evaluation states (PRD FR-ATT-003).
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Clocked in within the shift window (including grace period).</summary>
    Present = 0,

    /// <summary>No clock-in recorded and no approved leave or holiday.</summary>
    Absent = 1,

    /// <summary>Clocked in after the shift grace period.</summary>
    Late = 2,

    /// <summary>Worked less than the configured half-day threshold.</summary>
    HalfDay = 3,

    /// <summary>Covered by an approved leave request.</summary>
    OnLeave = 4,

    /// <summary>Public holiday — no attendance expected.</summary>
    Holiday = 5,
}
