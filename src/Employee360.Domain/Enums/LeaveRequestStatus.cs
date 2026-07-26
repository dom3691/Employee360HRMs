namespace Employee360.Domain.Enums;

/// <summary>
/// Leave request workflow states (PRD FR-LV-007/008/009).
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>Submitted and awaiting approver action.</summary>
    Pending = 0,

    /// <summary>Approved through all required approval steps.</summary>
    Approved = 1,

    /// <summary>Rejected by an approver.</summary>
    Rejected = 2,

    /// <summary>Sent back to the employee for changes before resubmission.</summary>
    ReturnedForRevision = 3,

    /// <summary>Withdrawn by the employee before final decision.</summary>
    Cancelled = 4,

    /// <summary>Escalated after the approval SLA elapsed without action.</summary>
    Escalated = 5,
}
