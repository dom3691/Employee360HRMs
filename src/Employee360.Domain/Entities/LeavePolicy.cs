using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Policy rules for a leave type (PRD FR-LV-002): entitlement, accrual,
/// carry-forward cap, and probation exclusion.
/// </summary>
public class LeavePolicy : AuditableEntity
{
    /// <summary>Owning leave type (1:1).</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>Navigation to the leave type.</summary>
    public LeaveType LeaveType { get; set; } = null!;

    /// <summary>Days granted per year.</summary>
    public decimal AnnualEntitlement { get; set; }

    /// <summary>How the entitlement accrues.</summary>
    public AccrualFrequency AccrualFrequency { get; set; } = AccrualFrequency.Annual;

    /// <summary>Maximum unused days carried into the next year (0 = none).</summary>
    public decimal CarryForwardMax { get; set; }

    /// <summary>Months of employment before the type can be used (0 = immediate).</summary>
    public int ProbationMonths { get; set; }
}
