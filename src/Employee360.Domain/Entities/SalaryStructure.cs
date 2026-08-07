using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Compensation template (FR-PAY-001): basic, housing, transport, and other allowances.
/// </summary>
public class SalaryStructure : AuditableEntity, ISoftDelete
{
    /// <summary>Template name, e.g. "Management Level 1".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Monthly basic salary (₦).</summary>
    public decimal Basic { get; set; }

    /// <summary>Monthly housing allowance (₦).</summary>
    public decimal Housing { get; set; }

    /// <summary>Monthly transport allowance (₦).</summary>
    public decimal Transport { get; set; }

    /// <summary>Other monthly allowances (₦).</summary>
    public decimal OtherAllowances { get; set; }

    /// <summary>When false, hidden from new assignments.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Comma-separated grade codes this structure applies to.</summary>
    public string? GradeCodes { get; set; }

    /// <summary>JSON array of salary components (PercentOfGross definitions).</summary>
    public string? ComponentsJson { get; set; }

    /// <summary>Monthly gross derived from components.</summary>
    public decimal GrossSalary => Basic + Housing + Transport + OtherAllowances;

    /// <summary>Employee salary assignments using this template.</summary>
    public ICollection<EmployeeSalary> EmployeeSalaries { get; set; } = new List<EmployeeSalary>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
