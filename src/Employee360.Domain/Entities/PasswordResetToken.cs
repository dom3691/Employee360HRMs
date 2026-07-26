using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Single-use password reset token (FR-AUTH-004: email token, expires in 1 hour).
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>Owning user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation to the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Opaque token value sent by email (cryptographically random, unique).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC instant the token expires (creation + 1 hour).</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>UTC instant the token was issued.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC instant the token was consumed, or null while unused.</summary>
    public DateTime? UsedAtUtc { get; set; }
}
