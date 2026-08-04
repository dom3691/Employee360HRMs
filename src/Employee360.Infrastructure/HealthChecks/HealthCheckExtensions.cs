using Employee360.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Employee360.Infrastructure.HealthChecks;

/// <summary>Registers health checks for DB, Hangfire, and Blob storage (Batch 17).</summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddEmployee360HealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<Employee360DbContext>(
                name: "database",
                tags: ["ready", "db"])
            .AddCheck<BlobStorageHealthCheck>(
                name: "blob-storage",
                tags: ["ready", "blob"])
            .AddCheck<HangfireHealthCheck>(
                name: "hangfire",
                tags: ["ready", "hangfire"]);

        return services;
    }
}
