namespace Employee360.Domain.Enums;

/// <summary>Job posting lifecycle (FR-REC-001).</summary>
public enum JobPostingStatus
{
    Draft = 0,
    Published = 1,
    Closed = 2,
}

/// <summary>
/// Recruitment pipeline stages (FR-REC-002):
/// Applied → Screening → Interview → Offer → Hired / Rejected.
/// </summary>
public enum CandidateStage
{
    Applied = 0,
    Screening = 1,
    Interview = 2,
    Offer = 3,
    Hired = 4,
    Rejected = 5,
}
