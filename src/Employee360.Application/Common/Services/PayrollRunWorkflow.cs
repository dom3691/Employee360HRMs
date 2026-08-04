using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Validates payroll run state transitions (FR-PAY-008..011).
/// </summary>
public static class PayrollRunWorkflow
{
    public static Result EnsureTransition(PayrollRunStatus current, PayrollRunStatus next)
    {
        var allowed = current switch
        {
            PayrollRunStatus.Draft => next is PayrollRunStatus.Calculated or PayrollRunStatus.Draft,
            PayrollRunStatus.Calculated => next is PayrollRunStatus.PendingApproval or PayrollRunStatus.Draft,
            PayrollRunStatus.PendingApproval => next is PayrollRunStatus.Approved or PayrollRunStatus.Calculated,
            PayrollRunStatus.Approved => next is PayrollRunStatus.Finalized or PayrollRunStatus.PendingApproval,
            PayrollRunStatus.Finalized => false,
            _ => false,
        };

        return allowed
            ? Result.Success()
            : Result.Failure($"Cannot transition payroll run from {current} to {next}.");
    }

    public static bool IsPeriodLocked(PayrollRunStatus status) =>
        status == PayrollRunStatus.Finalized;

    public static string BuildPeriodLabel(int year, int month) =>
        $"{new DateOnly(year, month, 1):MMMM yyyy}";
}
