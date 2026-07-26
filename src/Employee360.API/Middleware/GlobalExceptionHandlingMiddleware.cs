using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions into
/// RFC 7807 Problem Details responses (application/problem+json).
/// FluentValidation failures map to HTTP 400 with field-level errors;
/// everything else maps to HTTP 500 with a correlation trace identifier.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the next middleware, translating any thrown exception.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            await WriteValidationProblemAsync(context, validationException);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path} (TraceId: {TraceId})",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteServerErrorProblemAsync(context);
        }
    }

    private static async Task WriteValidationProblemAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    private static async Task WriteServerErrorProblemAsync(HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The request could not be processed. Contact support with the trace identifier.",
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }
}

/// <summary>Extension methods for registering <see cref="GlobalExceptionHandlingMiddleware"/>.</summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    /// <summary>Adds the global RFC 7807 exception handler to the pipeline.</summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
}
