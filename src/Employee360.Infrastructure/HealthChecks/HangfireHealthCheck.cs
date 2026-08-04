using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Employee360.Infrastructure.HealthChecks;

/// <summary>Verifies Hangfire job storage connectivity (Batch 17).</summary>
public sealed class HangfireHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public HangfireHealthCheck(IConfiguration configuration)
        => _configuration = configuration;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("Hangfire:Enabled", defaultValue: false))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("Hangfire is disabled in this environment."));
        }

        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            _ = monitoring.Servers();
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage is reachable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Hangfire storage is unreachable.", ex));
        }
    }
}
