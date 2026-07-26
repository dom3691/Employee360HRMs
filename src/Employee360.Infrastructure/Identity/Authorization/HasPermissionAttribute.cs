using Microsoft.AspNetCore.Authorization;

namespace Employee360.Infrastructure.Identity.Authorization;

/// <summary>
/// Declares that an endpoint requires a granular permission, e.g.
/// <c>[HasPermission(Permissions.Employees.Create)]</c>. Resolved dynamically by
/// <see cref="PermissionPolicyProvider"/> and enforced by
/// <see cref="PermissionAuthorizationHandler"/> against the JWT "permission" claims.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Prefix distinguishing permission policies from conventional ones.</summary>
    public const string PolicyPrefix = "Permission:";

    /// <summary>Requires the given permission name for the decorated endpoint.</summary>
    /// <param name="permission">A permission constant from <see cref="Domain.Constants.Permissions"/>.</param>
    public HasPermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
    }
}
