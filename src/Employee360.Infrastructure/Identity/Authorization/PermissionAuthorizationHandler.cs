using Microsoft.AspNetCore.Authorization;

namespace Employee360.Infrastructure.Identity.Authorization;

/// <summary>
/// Grants access when the authenticated principal carries a "permission" claim
/// matching the requirement. Permissions are embedded in the access token at
/// login from the user's role-permission assignments (least privilege, additive).
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == JwtTokenService.PermissionClaimType &&
            string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
