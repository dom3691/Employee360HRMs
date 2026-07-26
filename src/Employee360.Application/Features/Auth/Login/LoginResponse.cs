namespace Employee360.Application.Features.Auth.Login;

/// <summary>Successful authentication payload: token pair plus basic identity.</summary>
/// <param name="AccessToken">Signed JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">Access token expiry (UTC).</param>
/// <param name="RefreshToken">Opaque single-use refresh token.</param>
/// <param name="RefreshTokenExpiresAtUtc">Refresh token expiry (UTC).</param>
/// <param name="Email">Authenticated user's email.</param>
/// <param name="Roles">Role names held by the user.</param>
public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string Email,
    IReadOnlyCollection<string> Roles);
