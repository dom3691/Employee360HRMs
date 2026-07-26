using System.Text;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Interfaces;
using Employee360.Domain.Interfaces.Repositories;
using Employee360.Infrastructure.Identity;
using Employee360.Infrastructure.Identity.Authorization;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Persistence.Interceptors;
using Employee360.Infrastructure.Persistence.Repositories;
using Employee360.Infrastructure.Persistence.Seeding;
using Employee360.Infrastructure.Services;
using Employee360.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<Employee360DbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ---------------------------------------------------------------
        // Identity: JWT token service, manager scoping, seed data
        // ---------------------------------------------------------------
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IManagerScopeService, ManagerScopeService>();

        // Outbound email (SMTP; PRD Integration requirements).
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Seed data: roles, permission catalog, RBAC matrix (idempotent, background).
        services.AddScoped<IDataSeeder, IdentityDataSeeder>();
        services.AddHostedService<DataSeedHostedService>();

        // ---------------------------------------------------------------
        // Background jobs (later batches): Hangfire server + SQL storage
        // ---------------------------------------------------------------
        // services.AddHangfire(cfg => cfg.UseSqlServerStorage(
        //     configuration.GetConnectionString("HangfireConnection")));
        // services.AddHangfireServer();

        // ---------------------------------------------------------------
        // External services (later batches): Azure Blob storage
        // ---------------------------------------------------------------
        // services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        return services;
    }

    /// <summary>
    /// Configures JWT bearer authentication from the "Jwt" configuration section
    /// (FR-AUTH-001, NFR-SEC-003).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        return services;
    }

    /// <summary>
    /// Registers the dynamic permission-based authorization pipeline
    /// ([HasPermission] attribute + policy provider + claims handler, FR-AUTH-006/007).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
