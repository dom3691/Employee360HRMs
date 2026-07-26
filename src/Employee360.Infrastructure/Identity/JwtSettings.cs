namespace Employee360.Infrastructure.Identity;

/// <summary>Strongly-typed binding of the "Jwt" configuration section.</summary>
public sealed class JwtSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (iss claim).</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Token audience (aud claim).</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 signing key — sourced from Azure Key Vault in production.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Access token lifetime in minutes (default 15, NFR-SEC-003).</summary>
    public int AccessTokenExpiryMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days (default 7).</summary>
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
