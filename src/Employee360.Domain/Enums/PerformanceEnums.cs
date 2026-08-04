namespace Employee360.Domain.Enums;

/// <summary>Review cycle types (FR-PERF-001).</summary>
public enum ReviewCycleType
{
    Annual = 0,
    MidYear = 1,
    Probation = 2,
}

/// <summary>Review cycle lifecycle.</summary>
public enum ReviewCycleStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2,
}

/// <summary>Individual performance review workflow (FR-PERF-003..005).</summary>
public enum PerformanceReviewStatus
{
    NotStarted = 0,
    SelfAssessmentSubmitted = 1,
    ManagerReviewSubmitted = 2,
    Finalized = 3,
}
