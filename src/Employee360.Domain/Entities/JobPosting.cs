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
    public string? Requirements { get; set; }
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public DateOnly? ClosingDate { get; set; }
    public string? EmploymentType { get; set; }
    public string? Location { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public DateOnly? PostedDate { get; set; }
    public string? HiringManagerName { get; set; }
    public int ViewCount { get; set; }
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
