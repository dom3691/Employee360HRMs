using Microsoft.AspNetCore.Authorization;

namespace Employee360.Infrastructure.Identity.Authorization;

/// <summary>Authorization requirement carrying the permission name to enforce.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>The required permission name, e.g. "Leave.ApproveTeam".</summary>
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
