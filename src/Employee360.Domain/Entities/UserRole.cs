namespace Employee360.Domain.Entities;

/// <summary>
/// User ↔ Role assignment (composite key UserId + RoleId). Multi-role is
/// supported; permissions are additive (PRD FR-AUTH-007).
/// </summary>
public class UserRole
{
    /// <summary>The user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation to the user.</summary>
    public User User { get; set; } = null!;

    /// <summary>The assigned role.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Navigation to the role.</summary>
    public Role Role { get; set; } = null!;
}
