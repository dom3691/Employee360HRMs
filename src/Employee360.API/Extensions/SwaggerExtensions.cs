using Employee360.Application;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Employee360.API.Extensions;

/// <summary>Swagger/OpenAPI configuration helpers (Batch 17).</summary>
public static class SwaggerExtensions
{
    /// <summary>Maps API routes to module tags for Swagger UI grouping.</summary>
    private static readonly (string Prefix, string Module)[] ModulePrefixes =
    [
        ("api/v1/auth", "Authentication"),
        ("api/v1/employees", "Employees"),
        ("api/v1/departments", "Organization"),
        ("api/v1/positions", "Organization"),
        ("api/v1/grades", "Organization"),
        ("api/v1/company", "Administration"),
        ("api/v1/roles", "Administration"),
        ("api/v1/user-roles", "Administration"),
        ("api/v1/email-templates", "Administration"),
        ("api/v1/audit-logs", "Administration"),
        ("api/v1/leave", "Leave"),
        ("api/v1/attendance", "Attendance"),
        ("api/v1/shifts", "Attendance"),
        ("api/v1/payroll", "Payroll"),
        ("api/v1/salary-structures", "Payroll"),
        ("api/v1/reports", "Reports"),
        ("api/v1/reviews", "Performance"),
        ("api/v1/goals", "Performance"),
        ("api/v1/feedback", "Performance"),
        ("api/v1/jobs", "Recruitment"),
        ("api/v1/candidates", "Recruitment"),
        ("api/v1/onboarding", "Recruitment"),
        ("api/v1/self-service", "Self Service"),
        ("api/v1/notifications", "Self Service"),
        ("api/v1/profile-change-requests", "Self Service"),
    ];

    public static void AddEmployee360Swagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Employee360 HRMS API",
                Version = "v1",
                Description =
                    "Enterprise HR management platform — employee core, leave, attendance, payroll, " +
                    "recruitment, and performance for Nigerian organizations.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token obtained from POST /api/v1/auth/login.",
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

            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
            });

            options.TagActionsBy(api =>
            {
                var path = api.RelativePath ?? string.Empty;
                foreach (var (prefix, module) in ModulePrefixes)
                {
                    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return [module];
                    }
                }

                var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name)
                    ? name
                    : "General";
                return [controller ?? "General"];
            });

            options.DocInclusionPredicate((_, _) => true);
            options.OrderActionsBy(api => $"{ResolveModule(api.RelativePath)}_{api.HttpMethod}_{api.RelativePath}");
            options.OperationFilter<FileUploadOperationFilter>();

            IncludeXmlComments(options, typeof(Program).Assembly);
            IncludeXmlComments(options, typeof(DependencyInjection).Assembly);
        });
    }

    private static string ResolveModule(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "General";
        }

        foreach (var (prefix, module) in ModulePrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return module;
            }
        }

        return "General";
    }

    private static void IncludeXmlComments(SwaggerGenOptions options, System.Reflection.Assembly assembly)
    {
        var xmlFile = $"{assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}
