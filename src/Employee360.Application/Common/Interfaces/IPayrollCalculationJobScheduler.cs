namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Enqueues payroll calculation background jobs (Hangfire when enabled).
/// </summary>
public interface IPayrollCalculationJobScheduler
{
    /// <summary>Schedules calculation for the given run; returns job id when queued.</summary>
    Task<string?> ScheduleCalculationAsync(Guid payrollRunId, CancellationToken cancellationToken = default);
}
