namespace Employee360.API.Middleware;

/// <summary>
/// Adds standard security response headers (Batch 17 — API hardening).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["X-XSS-Protection"] = "0"; // deprecated; modern browsers rely on CSP

        // Minimal CSP — tighten per deployment when Angular assets are known.
        headers["Content-Security-Policy"] =
            "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

        await _next(context);
    }
}

/// <summary>Registers <see cref="SecurityHeadersMiddleware"/>.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
