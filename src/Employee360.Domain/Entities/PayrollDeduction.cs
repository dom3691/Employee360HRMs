using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Employee-specific recurring or one-off payroll deduction (FR-PAY-007).
/// </summary>
public class PayrollDeduction : AuditableEntity, ISoftDelete
{
    /// <summary>The employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Display name, e.g. "Staff Cooperative".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Category (loan, cooperative, union, custom).</summary>
    public PayrollDeductionType DeductionType { get; set; } = PayrollDeductionType.Custom;

    /// <summary>Fixed monthly deduction amount (₦).</summary>
    public decimal? FixedAmount { get; set; }

    /// <summary>Percentage of gross salary, when not using a fixed amount.</summary>
    public decimal? PercentOfGross { get; set; }

    /// <summary>True for ongoing deductions until end date.</summary>
    public bool IsRecurring { get; set; } = true;

    /// <summary>First payroll period this deduction applies.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Last payroll period, or null when open-ended.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>When false, skipped in payroll calculation.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
