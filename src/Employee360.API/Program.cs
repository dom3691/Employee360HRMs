using Employee360.API.Middleware;
using Employee360.Application;
using Employee360.Infrastructure;
using Hangfire;
using Microsoft.OpenApi.Models;
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

    // =======================================================================
    // 1. LOGGING — Serilog (configuration-driven; App Insights sink via config)
    // =======================================================================
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Employee360.API"));

    // =======================================================================
    // 2. LAYERS — Application (MediatR, validators, mappings) + Infrastructure
    //    (EF Core, repositories, identity, Hangfire, email, blob storage)
    // =======================================================================
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // =======================================================================
    // 3. AUTHENTICATION & AUTHORIZATION — JWT Bearer + granular RBAC policies
    // =======================================================================
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddPermissionAuthorization();

    // =======================================================================
    // 4. API — Controllers, versioned routes, JSON options
    // =======================================================================
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
    // 6. SWAGGER / OPENAPI — with JWT bearer security definition
    // =======================================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Employee360 HRMS API",
            Version = "v1",
            Description = "Enterprise HR management platform — employee core, leave, attendance, and payroll for Nigerian organizations.",
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token.",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });

        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // =======================================================================
    // 7. BACKGROUND JOBS — Hangfire server/storage registered in
    //    AddInfrastructure when "Hangfire:Enabled" is true.
    // =======================================================================
    var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", defaultValue: false);

    var app = builder.Build();

    // =======================================================================
    // HTTP PIPELINE (order matters)
    // =======================================================================
    app.UseGlobalExceptionHandling();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee360 HRMS API v1");
        });
    }

    app.UseHttpsRedirection();

    app.UseCors(CorsPolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    if (hangfireEnabled)
    {
        app.UseHangfireDashboard(
            app.Configuration.GetValue("Hangfire:DashboardPath", "/hangfire"));

        // Recurring leave jobs (FR-LV-002 accrual, FR-LV-009 escalation).
        RecurringJob.AddOrUpdate<Employee360.Infrastructure.BackgroundJobs.LeaveAccrualJob>(
            "leave-accrual",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(2)); // 02:00 daily

        RecurringJob.AddOrUpdate<Employee360.Infrastructure.BackgroundJobs.LeaveEscalationJob>(
            "leave-escalation",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly());
    }

    app.MapControllers();

    // Seed data (roles, permission catalog, RBAC matrix) runs in the background
    // via DataSeedHostedService, registered in AddInfrastructure.

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
