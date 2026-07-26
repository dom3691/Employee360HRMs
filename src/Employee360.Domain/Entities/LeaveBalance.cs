using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// An employee's balance for one leave type in one year — unique per
/// Employee + LeaveType + Year (PRD FR-LV-004).
/// </summary>
public class LeaveBalance : AuditableEntity
{
    /// <summary>Owning employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>The leave type.</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>Navigation to the leave type.</summary>
    public LeaveType LeaveType { get; set; } = null!;

    /// <summary>Calendar year the balance applies to.</summary>
    public int Year { get; set; }

    /// <summary>Days entitled this year (accrued to date for monthly policies).</summary>
    public decimal Entitled { get; set; }

    /// <summary>Days consumed by approved leave.</summary>
    public decimal Used { get; set; }

    /// <summary>Days held by pending (unapproved) requests.</summary>
    public decimal Pending { get; set; }

    /// <summary>Days carried forward from the previous year (capped by policy).</summary>
    public decimal CarriedForward { get; set; }

    /// <summary>Days currently available to request (not mapped).</summary>
    public decimal Available => Entitled + CarriedForward - Used - Pending;
}
