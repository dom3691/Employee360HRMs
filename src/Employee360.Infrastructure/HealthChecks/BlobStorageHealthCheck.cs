using Azure.Storage.Blobs;
using Employee360.Infrastructure.Services.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Employee360.Infrastructure.HealthChecks;

/// <summary>Verifies Azure Blob Storage connectivity (Batch 17).</summary>
public sealed class BlobStorageHealthCheck : IHealthCheck
{
    private readonly BlobStorageSettings _settings;

    public BlobStorageHealthCheck(IOptions<BlobStorageSettings> settings)
        => _settings = settings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            return HealthCheckResult.Degraded("AzureBlobStorage:ConnectionString is not configured.");
        }

        try
        {
            var client = new BlobServiceClient(_settings.ConnectionString);
            await client.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Blob storage is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage is unreachable.", ex);
        }
    }
}
