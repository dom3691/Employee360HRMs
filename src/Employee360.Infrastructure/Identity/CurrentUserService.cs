using System.Security.Claims;
using Employee360.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Employee360.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from the HTTP request's JWT claims principal.
/// Returns anonymous values (null/empty) outside an HTTP request — e.g. in
/// Hangfire background jobs — so audit rows record a system actor.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    /// <summary>Custom claim carrying the linked employee record id.</summary>
    public const string EmployeeIdClaimType = "employee_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    /// <inheritdoc />
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public Guid? EmployeeId =>
        Guid.TryParse(Principal?.FindFirstValue(EmployeeIdClaimType), out var id)
            ? id
            : null;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    /// <inheritdoc />
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    /// <inheritdoc />
    public bool HasPermission(string permission) =>
        Principal?.HasClaim(JwtTokenService.PermissionClaimType, permission) ?? false;
}
