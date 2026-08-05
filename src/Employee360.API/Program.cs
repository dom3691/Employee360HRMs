using Employee360.API.Extensions;
using Employee360.API.Middleware;
using Employee360.Application;
using Employee360.Infrastructure;
using Employee360.Infrastructure.Configuration;
using Employee360.Infrastructure.HealthChecks;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

// ---------------------------------------------------------------------------
// Bootstrap logger: captures startup failures before the host is built.
// ---------------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Employee360 API");

    var builder = WebApplication.CreateBuilder(args);

    // Render/Railway/Fly.io: bind to the platform-assigned PORT.
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    // =======================================================================
    // 0. KEY VAULT — optional; secrets override appsettings when configured
    // =======================================================================
    builder.Configuration.AddEmployee360KeyVault(builder.Configuration["KeyVault:Uri"]);

    // =======================================================================
    // 1. LOGGING — Serilog (configuration-driven; App Insights sink via config)
    // =======================================================================
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Employee360.API"));

    // Application Insights telemetry (Batch 17).
    var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(appInsightsConnection))
    {
        builder.Services.AddApplicationInsightsTelemetry();
    }

    // =======================================================================
    // 2. LAYERS — Application + Infrastructure
    // =======================================================================
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddEmployee360HealthChecks();

    // =======================================================================
    // 3. AUTHENTICATION & AUTHORIZATION
    // =======================================================================
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddPermissionAuthorization();

    // =======================================================================
    // 4. API HARDENING — rate limiting, response caching (Batch 17)
    // =======================================================================
    builder.Services.AddEmployee360ApiHardening();
    builder.Services.AddControllers();

    // =======================================================================
    // 5. CORS — Angular SPA origins from configuration
    // =======================================================================
    const string CorsPolicyName = "Employee360Cors";
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
        options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    // =======================================================================
    // 6. SWAGGER / OPENAPI — JWT auth, module grouping, XML comments
    // =======================================================================
    builder.Services.AddEmployee360Swagger();

    var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", defaultValue: false);
    var swaggerEnabled = builder.Configuration.GetValue(
        "Swagger:Enabled",
        builder.Environment.IsDevelopment());

    var app = builder.Build();

    // One-shot demo seed (scripts/seed-demo-data.ps1).
    if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
    {
        using var scope = app.Services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<Employee360.Application.Common.Interfaces.IDataSeeder>();
        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }

        Log.Information("Demo data seed completed via --seed-demo");
        return;
    }

    // =======================================================================
    // HTTP PIPELINE (order matters)
    // =======================================================================
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseGlobalExceptionHandling();
    app.UseSecurityHeaders();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                diagnosticContext.Set("UserEmail", httpContext.User.FindFirst("email")?.Value);
            }
        };
    });

    if (swaggerEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee360 HRMS API v1");
            options.DocumentTitle = "Employee360 HRMS API";
        });
    }

    app.UseHttpsRedirection();
    app.UseResponseCaching();
    app.UseRateLimiter();
    app.UseCors(CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();

    if (hangfireEnabled)
    {
        try
        {
            app.UseHangfireDashboard(
                app.Configuration.GetValue("Hangfire:DashboardPath", "/hangfire"));

            RecurringJob.AddOrUpdate<Employee360.Infrastructure.BackgroundJobs.LeaveAccrualJob>(
                "leave-accrual",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(2));

            RecurringJob.AddOrUpdate<Employee360.Infrastructure.BackgroundJobs.LeaveEscalationJob>(
                "leave-escalation",
                job => job.RunAsync(CancellationToken.None),
                Cron.Hourly());

            RecurringJob.AddOrUpdate<Employee360.Infrastructure.BackgroundJobs.DailyStatusCalculationJob>(
                "attendance-daily-status",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily(1));
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Hangfire recurring jobs could not be registered; " +
                "verify HangfireConnection and that the database exists. API will continue without background jobs.");
        }
    }

    app.MapControllers()
        .RequireRateLimiting(ApiHardeningExtensions.ApiRateLimitPolicy);

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                status = report.Status.ToString(),
                duration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                }),
            };
            await context.Response.WriteAsJsonAsync(payload);
        },
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Employee360 API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed for integration testing with WebApplicationFactory.</summary>
public partial class Program;
