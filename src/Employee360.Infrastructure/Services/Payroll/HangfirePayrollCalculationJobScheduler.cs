using Employee360.Application.Common.Interfaces;
using Employee360.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Services.Payroll;

/// <summary>
/// Enqueues payroll calculation on Hangfire when enabled; otherwise runs inline.
/// </summary>
public sealed class HangfirePayrollCalculationJobScheduler : IPayrollCalculationJobScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfirePayrollCalculationJobScheduler> _logger;

    public HangfirePayrollCalculationJobScheduler(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<HangfirePayrollCalculationJobScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> ScheduleCalculationAsync(
        Guid payrollRunId,
        CancellationToken cancellationToken = default)
    {
        if (_configuration.GetValue("Hangfire:Enabled", defaultValue: false))
        {
            var jobId = BackgroundJob.Enqueue<CalculatePayrollJob>(
                job => job.RunAsync(payrollRunId, CancellationToken.None));

            _logger.LogInformation(
                "Queued payroll calculation job {JobId} for run {RunId}",
                jobId,
                payrollRunId);

            return jobId;
        }

        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CalculatePayrollJob>();
        await job.RunAsync(payrollRunId, cancellationToken);

        _logger.LogInformation("Ran payroll calculation synchronously for run {RunId}", payrollRunId);
        return null;
    }
}
