using Employee360.API.Middleware;
using Employee360.Application;
using Employee360.Infrastructure;
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
    // 3. AUTHENTICATION & AUTHORIZATION — JWT Bearer + RBAC policies
    //    (wired in Batch 3; placeholder kept here so ordering is stable)
    // =======================================================================
    // builder.Services.AddJwtAuthentication(builder.Configuration);
    // builder.Services.AddPermissionAuthorization();

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
    // 7. BACKGROUND JOBS — Hangfire dashboard & recurring jobs
    //    (server + storage registered in Infrastructure from Batch 3)
    // =======================================================================
    // builder.Services.AddHangfireDashboardAuthorization();

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

    // app.UseAuthentication();   // Batch 3
    app.UseAuthorization();

    // app.UseHangfireDashboard("/hangfire");   // Batch 3+

    app.MapControllers();

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
