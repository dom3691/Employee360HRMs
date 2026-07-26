using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// RBAC role (PRD Section 6). Users may hold multiple roles; permissions are additive.
/// </summary>
public class Role : AuditableEntity
{
    /// <summary>Unique role name (see <see cref="Constants.RoleNames"/> for system roles).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable description of the role's responsibility.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>True for the seven seeded system roles, which cannot be deleted.</summary>
    public bool IsSystemRole { get; set; }

    /// <summary>Granular permissions granted to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>Users holding this role.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
