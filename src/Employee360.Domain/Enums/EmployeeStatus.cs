namespace Employee360.Domain.Enums;

/// <summary>
/// Employee lifecycle states (PRD FR-EMP-004:
/// Draft → Active → On Leave / Suspended → Resigned / Terminated).
/// </summary>
public enum EmployeeStatus
{
    /// <summary>Record created but onboarding not completed; cannot log in.</summary>
    Draft = 0,

    /// <summary>Actively employed and able to use the system.</summary>
    Active = 1,

    /// <summary>Temporarily away on approved extended leave.</summary>
    OnLeave = 2,

    /// <summary>Suspended pending investigation or sanction; login revoked.</summary>
    Suspended = 3,

    /// <summary>Voluntarily exited; record retained for audit.</summary>
    Resigned = 4,

    /// <summary>Involuntarily exited; record retained for audit.</summary>
    Terminated = 5,
}
