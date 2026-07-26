namespace Employee360.Domain.Enums;

/// <summary>
/// Payroll run workflow states (PRD FR-PAY-008:
/// Draft → Calculated → PendingApproval → Approved → Finalized).
/// A finalized run locks its period against modification (FR-PAY-011).
/// </summary>
public enum PayrollRunStatus
{
    /// <summary>Run initiated; calculation not yet performed.</summary>
    Draft = 0,

    /// <summary>All employee pay computed; exceptions available for review.</summary>
    Calculated = 1,

    /// <summary>Submitted and awaiting HR Manager / Finance approval.</summary>
    PendingApproval = 2,

    /// <summary>Approved for disbursement.</summary>
    Approved = 3,

    /// <summary>Payslips generated, bank file exported, period locked.</summary>
    Finalized = 4,
}
