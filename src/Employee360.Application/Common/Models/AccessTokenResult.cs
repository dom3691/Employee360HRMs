namespace Employee360.Application.Common.Models;

/// <summary>A generated JWT access token and its expiry.</summary>
/// <param name="Token">The serialized JWT.</param>
/// <param name="ExpiresAtUtc">UTC instant the token expires.</param>
public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>A generated opaque refresh token and its expiry.</summary>
/// <param name="Token">The opaque token value.</param>
/// <param name="ExpiresAtUtc">UTC instant the token expires.</param>
public sealed record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);
