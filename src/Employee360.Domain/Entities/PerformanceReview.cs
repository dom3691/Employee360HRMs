using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>Employee performance review for a cycle (FR-PERF-003..005).</summary>
public class PerformanceReview : AuditableEntity
{
    public Guid ReviewCycleId { get; set; }
    public ReviewCycle ReviewCycle { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid? ManagerEmployeeId { get; set; }
    public Employee? ManagerEmployee { get; set; }

    public decimal? SelfRating { get; set; }
    public string? SelfComments { get; set; }
    public DateTime? SelfSubmittedAtUtc { get; set; }

    public decimal? ManagerRating { get; set; }
    public string? ManagerComments { get; set; }
    public DateTime? ManagerSubmittedAtUtc { get; set; }

    public decimal? FinalRating { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public PerformanceReviewStatus Status { get; set; } = PerformanceReviewStatus.NotStarted;

    public ICollection<PeerFeedback> PeerFeedbacks { get; set; } = new List<PeerFeedback>();
}
