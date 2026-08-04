using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Application.Features.Recruitment;

/// <summary>Validates candidate pipeline stage transitions (FR-REC-002).</summary>
public static class CandidatePipelineWorkflow
{
    public static Result EnsureTransition(CandidateStage current, CandidateStage next)
    {
        if (current == next)
        {
            return Result.Success();
        }

        if (current is CandidateStage.Hired or CandidateStage.Rejected)
        {
            return Result.Failure($"Cannot transition from terminal stage {current}.");
        }

        if (next == CandidateStage.Rejected)
        {
            return Result.Success();
        }

        var allowed = current switch
        {
            CandidateStage.Applied => next is CandidateStage.Screening,
            CandidateStage.Screening => next is CandidateStage.Interview,
            CandidateStage.Interview => next is CandidateStage.Offer,
            CandidateStage.Offer => next is CandidateStage.Hired,
            _ => false,
        };

        return allowed
            ? Result.Success()
            : Result.Failure($"Cannot transition candidate from {current} to {next}.");
    }

    public static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
