using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Application.Features.Performance;

/// <summary>Review cycle state transitions (FR-PERF-001).</summary>
public static class ReviewCycleWorkflow
{
    public static Result EnsureTransition(ReviewCycleStatus current, ReviewCycleStatus next)
    {
        var allowed = current switch
        {
            ReviewCycleStatus.Draft => next is ReviewCycleStatus.Active,
            ReviewCycleStatus.Active => next is ReviewCycleStatus.Closed,
            ReviewCycleStatus.Closed => false,
            _ => false,
        };

        return allowed
            ? Result.Success()
            : Result.Failure($"Cannot transition review cycle from {current} to {next}.");
    }

    public static Result EnsureWeightsSumTo100(decimal goal, decimal self, decimal manager, decimal peer)
    {
        var total = goal + self + manager + peer;
        return Math.Abs(total - 100m) <= 0.01m
            ? Result.Success()
            : Result.Failure($"Rating weights must sum to 100 (current total: {total}).");
    }
}

/// <summary>Performance review workflow rules (FR-PERF-003..005).</summary>
public static class PerformanceReviewWorkflow
{
    public static Result EnsureSelfSubmission(PerformanceReviewStatus status, ReviewCycleStatus cycleStatus)
    {
        if (cycleStatus != ReviewCycleStatus.Active)
        {
            return Result.Failure("Self-assessment is only allowed in an active review cycle.");
        }

        return status is PerformanceReviewStatus.NotStarted
            ? Result.Success()
            : Result.Failure("Self-assessment has already been submitted.");
    }

    public static Result EnsureManagerSubmission(PerformanceReviewStatus status, ReviewCycleStatus cycleStatus)
    {
        if (cycleStatus != ReviewCycleStatus.Active)
        {
            return Result.Failure("Manager review is only allowed in an active review cycle.");
        }

        return status is PerformanceReviewStatus.SelfAssessmentSubmitted
            ? Result.Success()
            : Result.Failure("Manager review requires a submitted self-assessment.");
    }

    public static Result EnsureFinalization(PerformanceReviewStatus status)
    {
        return status is PerformanceReviewStatus.ManagerReviewSubmitted
            ? Result.Success()
            : Result.Failure("Final rating requires a submitted manager review.");
    }
}
