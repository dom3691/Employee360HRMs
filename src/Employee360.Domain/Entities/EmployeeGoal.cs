using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>Employee goal for a review cycle (FR-PERF-002).</summary>
public class EmployeeGoal : AuditableEntity
{
    public Guid ReviewCycleId { get; set; }
    public ReviewCycle ReviewCycle { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Relative weight within the employee's goal set (0-100).</summary>
    public decimal Weight { get; set; }

    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
}
