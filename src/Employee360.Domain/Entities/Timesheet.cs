using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Weekly timesheet for non-shift staff (PRD FR-ATT-004).
/// Unique on (EmployeeId, WeekStart).
/// </summary>
public class Timesheet : AuditableEntity
{
    /// <summary>The employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Monday of the week covered.</summary>
    public DateOnly WeekStart { get; set; }

    /// <summary>Total hours logged for the week.</summary>
    public decimal TotalHours { get; set; }

    /// <summary>Approval workflow status.</summary>
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    /// <summary>Optional notes from the employee.</summary>
    public string? Notes { get; set; }

    /// <summary>Manager/HR comments on approval or rejection.</summary>
    public string? ReviewComments { get; set; }
}
