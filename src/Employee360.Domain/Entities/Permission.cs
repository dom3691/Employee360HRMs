using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Granular permission (module + action), e.g. "Employees.Create".
/// The catalog is seeded from <see cref="Constants.Permissions"/>.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Unique permission name, e.g. "Leave.ApproveTeam".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Owning module, e.g. "Leave" (used for grouping in admin UI).</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Human-readable description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Roles granted this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
