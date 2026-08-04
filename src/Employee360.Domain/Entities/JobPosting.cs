using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>Open job requisition (FR-REC-001).</summary>
public class JobPosting : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Description { get; set; } = string.Empty;
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public DateOnly? ClosingDate { get; set; }
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
