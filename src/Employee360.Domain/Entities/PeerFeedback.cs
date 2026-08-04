using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Anonymous 360-degree peer feedback (FR-PERF-006 Should).
/// Reviewer identity is stored for deduplication but never exposed via API.
/// </summary>
public class PeerFeedback : AuditableEntity
{
    public Guid PerformanceReviewId { get; set; }
    public PerformanceReview PerformanceReview { get; set; } = null!;

    /// <summary>Internal only — used to prevent duplicate submissions, not returned in API.</summary>
    public Guid ReviewerEmployeeId { get; set; }

    public decimal Rating { get; set; }
    public string? Comments { get; set; }
}
