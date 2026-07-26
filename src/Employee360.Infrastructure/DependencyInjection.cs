using Employee360.Domain.Interfaces;
using Employee360.Domain.Interfaces.Repositories;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Persistence.Interceptors;
using Employee360.Infrastructure.Persistence.Repositories;
using Employee360.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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
        // Cross-cutting services
        // ---------------------------------------------------------------
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // ---------------------------------------------------------------
        // Persistence: DbContext (+ audit interceptor), repositories, UoW
        // ---------------------------------------------------------------
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<Employee360DbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(maxRetryCount: 3));

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<Employee360DbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ---------------------------------------------------------------
        // Identity & JWT (Batch 4+): token service, password hasher
        // ---------------------------------------------------------------
        // services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ---------------------------------------------------------------
        // Background jobs (later batches): Hangfire server + SQL storage
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
