using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>Applicant in a job posting pipeline (FR-REC-002..004).</summary>
public class Candidate : AuditableEntity
{
    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public CandidateStage Stage { get; set; } = CandidateStage.Applied;
    public string? ResumePath { get; set; }

    /// <summary>Set when converted to an employee record (FR-REC-005).</summary>
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Recruiter rating (1-5).</summary>
    public decimal? Rating { get; set; }

    /// <summary>Current job title supplied at application.</summary>
    public string? CurrentTitle { get; set; }

    /// <summary>JSON array of recruiter tags.</summary>
    public string? TagsJson { get; set; }
}
