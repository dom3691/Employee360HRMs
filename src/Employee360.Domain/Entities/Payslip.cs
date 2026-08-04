using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Itemized employee pay record for a payroll run (FR-PAY-009).
/// </summary>
public class Payslip : AuditableEntity
{
    /// <summary>Parent payroll run.</summary>
    public Guid PayrollRunId { get; set; }

    /// <summary>Navigation to the payroll run.</summary>
    public PayrollRun PayrollRun { get; set; } = null!;

    /// <summary>The employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Snapshot employee code at calculation time.</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>Snapshot employee name at calculation time.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    public decimal Basic { get; set; }
    public decimal Housing { get; set; }
    public decimal Transport { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal GrossPay { get; set; }
    public decimal PayFactor { get; set; } = 1.0m;
    public decimal CraMonthly { get; set; }
    public decimal Paye { get; set; }
    public decimal PensionEmployee { get; set; }
    public decimal PensionEmployer { get; set; }
    public decimal Nhf { get; set; }
    public decimal NsitfEmployer { get; set; }
    public decimal CustomDeductionsTotal { get; set; }
    public decimal NetPay { get; set; }

    /// <summary>JSON snapshot of custom deduction line items.</summary>
    public string? CustomDeductionsJson { get; set; }

    /// <summary>Relative blob path for the PDF payslip.</summary>
    public string? PdfPath { get; set; }
}
