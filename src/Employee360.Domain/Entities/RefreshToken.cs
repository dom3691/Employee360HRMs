using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Persisted refresh token supporting the JWT refresh flow (FR-AUTH-001).
/// Tokens are single-use: rotation revokes the old token and records its successor.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>Owning user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation to the owning user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Opaque token value (cryptographically random, unique).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC instant the token expires.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>UTC instant the token was issued.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC instant the token was revoked, or null while valid.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Token that replaced this one during rotation, when applicable.</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>True when the token is neither revoked nor expired (not mapped).</summary>
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
