using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>Performance review period configuration (FR-PERF-001).</summary>
public class ReviewCycle : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public ReviewCycleType Type { get; set; } = ReviewCycleType.Annual;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ReviewCycleStatus Status { get; set; } = ReviewCycleStatus.Draft;

    /// <summary>Weight for goal achievement in final rating (0-100).</summary>
    public decimal GoalWeightPercent { get; set; } = 70m;

    /// <summary>Weight for self-assessment rating (0-100).</summary>
    public decimal SelfWeightPercent { get; set; } = 15m;

    /// <summary>Weight for manager rating (0-100).</summary>
    public decimal ManagerWeightPercent { get; set; } = 15m;

    /// <summary>Weight for 360 peer feedback (0-100, FR-PERF-006 Should).</summary>
    public decimal PeerWeightPercent { get; set; }

    public ICollection<EmployeeGoal> Goals { get; set; } = new List<EmployeeGoal>();
    public ICollection<PerformanceReview> Reviews { get; set; } = new List<PerformanceReview>();
}
