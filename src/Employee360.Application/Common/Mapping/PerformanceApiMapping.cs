using Employee360.Domain.Enums;

namespace Employee360.Application.Common.Mapping;

/// <summary>Maps domain performance enums to frontend contract string values.</summary>
public static class PerformanceApiMapping
{
    public static string ToApiStatus(PerformanceReviewStatus status) => status switch
    {
        PerformanceReviewStatus.NotStarted => "NotStarted",
        PerformanceReviewStatus.SelfAssessmentSubmitted => "SelfAssessment",
        PerformanceReviewStatus.ManagerReviewSubmitted => "ManagerReview",
        PerformanceReviewStatus.Finalized => "Complete",
        _ => status.ToString(),
    };

    public static string ToApiCycleType(ReviewCycleType type) => type switch
    {
        ReviewCycleType.Annual => "Annual",
        ReviewCycleType.MidYear => "MidYear",
        ReviewCycleType.Probation => "Probation",
        _ => type.ToString(),
    };

    public static string ToApiCycleStatus(ReviewCycleStatus status) => status switch
    {
        ReviewCycleStatus.Draft => "Draft",
        ReviewCycleStatus.Active => "Active",
        ReviewCycleStatus.Closed => "Closed",
        _ => status.ToString(),
    };

    public static string ToApiJobStatus(JobPostingStatus status) => status switch
    {
        JobPostingStatus.Draft => "Draft",
        JobPostingStatus.Published => "Published",
        JobPostingStatus.Closed => "Closed",
        _ => status.ToString(),
    };
}
