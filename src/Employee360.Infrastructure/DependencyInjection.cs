using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Employee360.Infrastructure;

/// <summary>
/// Registers all Infrastructure-layer services (EF Core, repositories, identity/JWT,
/// Hangfire, email, and blob storage) into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure-layer services. Called once from the API composition root.
    /// Registration blocks are added incrementally as feature batches are implemented.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration (connection strings, JWT, SMTP, etc.).</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------------------------------------------------------------
        // Persistence (Batch 2): DbContext, interceptors, repositories
        // ---------------------------------------------------------------
        // services.AddDbContext<Employee360DbContext>(options =>
        //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // ---------------------------------------------------------------
        // Identity & JWT (Batch 3): token service, password hasher
        // ---------------------------------------------------------------
        // services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ---------------------------------------------------------------
        // Background jobs (Batch 3+): Hangfire server + SQL storage
        // ---------------------------------------------------------------
        // services.AddHangfire(cfg => cfg.UseSqlServerStorage(
        //     configuration.GetConnectionString("HangfireConnection")));
        // services.AddHangfireServer();

        // ---------------------------------------------------------------
        // External services (later batches): SMTP email, Azure Blob storage
        // ---------------------------------------------------------------
        // services.AddScoped<IEmailService, SmtpEmailService>();
        // services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        return services;
    }
}
