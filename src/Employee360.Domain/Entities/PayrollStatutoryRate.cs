using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Configurable statutory payroll rate (CRA, Pension, NHF, NSITF) per tax year
/// (FR-PAY-003..006). No hard-coded rates — updated when Finance Act changes.
/// </summary>
public class PayrollStatutoryRate : AuditableEntity
{
    /// <summary>Unique code, e.g. "PENSION_EMPLOYEE", "CRA_FIXED_MINIMUM".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable label.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tax year this rate applies to.</summary>
    public int TaxYear { get; set; }

    /// <summary>Employee contribution rate (decimal fraction), if applicable.</summary>
    public decimal? EmployeeRate { get; set; }

    /// <summary>Employer contribution rate (decimal fraction), if applicable.</summary>
    public decimal? EmployerRate { get; set; }

    /// <summary>Fixed annual amount (e.g. CRA ₦200,000 minimum).</summary>
    public decimal? FixedAnnualAmount { get; set; }

    /// <summary>Variable rate applied to basis (e.g. CRA 1% or 20% of gross).</summary>
    public decimal? VariableRate { get; set; }

    /// <summary>Basis for percentage calculations.</summary>
    public StatutoryRateBasis Basis { get; set; } = StatutoryRateBasis.Gross;

    /// <summary>When false, excluded from calculation.</summary>
    public bool IsActive { get; set; } = true;
}
