using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Employee pay assignment with effective-date history (FR-PAY-002).
/// </summary>
public class EmployeeSalary : AuditableEntity
{
    /// <summary>The employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Source template, if assigned from a structure.</summary>
    public Guid? SalaryStructureId { get; set; }

    /// <summary>Navigation to the salary structure.</summary>
    public SalaryStructure? SalaryStructure { get; set; }

    /// <summary>Monthly basic salary (₦).</summary>
    public decimal Basic { get; set; }

    /// <summary>Monthly housing allowance (₦).</summary>
    public decimal Housing { get; set; }

    /// <summary>Monthly transport allowance (₦).</summary>
    public decimal Transport { get; set; }

    /// <summary>Other monthly allowances (₦).</summary>
    public decimal OtherAllowances { get; set; }

    /// <summary>First day this assignment is effective (inclusive).</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Last day this assignment is effective, or null when current.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Monthly gross derived from components.</summary>
    public decimal GrossSalary => Basic + Housing + Transport + OtherAllowances;
}
