using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Monthly payroll run workflow (FR-PAY-008..011): Draft → Calculated →
/// PendingApproval → Approved → Finalized.
/// </summary>
public class PayrollRun : AuditableEntity
{
    /// <summary>Pay period year, e.g. 2026.</summary>
    public int PeriodYear { get; set; }

    /// <summary>Pay period month (1-12).</summary>
    public int PeriodMonth { get; set; }

    /// <summary>First day of the pay period (inclusive).</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Last day of the pay period (inclusive).</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Human-readable label, e.g. "August 2026".</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    /// <summary>Tax year used for PAYE/statutory config.</summary>
    public int TaxYear { get; set; }

    /// <summary>Current workflow status.</summary>
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    /// <summary>User who initiated the run.</summary>
    public Guid? RunByUserId { get; set; }

    /// <summary>User who approved the run.</summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>UTC instant the run was submitted for approval.</summary>
    public DateTime? SubmittedAtUtc { get; set; }

    /// <summary>UTC instant the run was approved.</summary>
    public DateTime? ApprovedAtUtc { get; set; }

    /// <summary>UTC instant the period was finalized and locked (FR-PAY-011).</summary>
    public DateTime? FinalizedAtUtc { get; set; }

    /// <summary>Total employees to process during calculation.</summary>
    public int TotalEmployees { get; set; }

    /// <summary>Employees processed so far (Hangfire progress).</summary>
    public int ProcessedEmployees { get; set; }

    /// <summary>Last calculation error message, cleared on success.</summary>
    public string? LastError { get; set; }

    /// <summary>Hangfire background job id for the calculation job.</summary>
    public string? CalculationJobId { get; set; }

    /// <summary>Generated payslips for this run.</summary>
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
