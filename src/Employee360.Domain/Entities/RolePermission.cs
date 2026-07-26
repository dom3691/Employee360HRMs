namespace Employee360.Domain.Entities;

/// <summary>
/// Role ↔ Permission join (composite key RoleId + PermissionId).
/// </summary>
public class RolePermission
{
    /// <summary>The role.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Navigation to the role.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>The granted permission.</summary>
    public Guid PermissionId { get; set; }

    /// <summary>Navigation to the permission.</summary>
    public Permission Permission { get; set; } = null!;
}
