using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Employee360.API.Extensions;

/// <summary>Rate limiting and response caching configuration (Batch 17).</summary>
public static class ApiHardeningExtensions
{
    public const string AuthRateLimitPolicy = "auth";
    public const string ApiRateLimitPolicy = "api";

    public static IServiceCollection AddEmployee360ApiHardening(this IServiceCollection services)
    {
        services.AddResponseCaching();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(ApiRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
