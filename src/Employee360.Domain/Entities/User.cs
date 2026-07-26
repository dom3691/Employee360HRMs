using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Authentication account (PRD data model: User 1:1 Employee).
/// Login/lockout mechanics (FR-AUTH-001..004) are implemented by the Auth slices.
/// </summary>
public class User : AuditableEntity, ISoftDelete
{
    /// <summary>Login email (unique).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2/bcrypt password hash — never the plain-text password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Linked employee record, when this account belongs to an employee.</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Navigation to the linked employee.</summary>
    public Employee? Employee { get; set; }

    /// <summary>False disables login regardless of credentials (FR-EMP deactivation).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp of the last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Consecutive failed login attempts (lockout after 5, FR-AUTH-003).</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>UTC instant until which the account is locked out, or null when not locked.</summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>Role assignments.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>Issued refresh tokens.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
