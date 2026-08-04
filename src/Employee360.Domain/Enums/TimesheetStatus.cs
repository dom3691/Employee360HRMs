namespace Employee360.Domain.Enums;

/// <summary>Weekly timesheet workflow states (FR-ATT-004).</summary>
public enum TimesheetStatus
{
    /// <summary>Employee is still editing.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting manager approval.</summary>
    Submitted = 1,

    /// <summary>Approved by manager/HR.</summary>
    Approved = 2,

    /// <summary>Rejected — employee must revise.</summary>
    Rejected = 3,
}
