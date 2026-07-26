using System.Security.Claims;
using Employee360.Application.Common.Models;

namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Issues and inspects JWT access tokens and opaque refresh tokens (FR-AUTH-001).
/// Implemented in Infrastructure over System.IdentityModel.Tokens.Jwt.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed access token embedding identity, role, and permission claims.
    /// </summary>
    /// <param name="userId">The authenticated user's id (NameIdentifier claim).</param>
    /// <param name="email">The user's email (Email claim).</param>
    /// <param name="employeeId">Linked employee id, embedded as the employee_id claim when present.</param>
    /// <param name="roles">Role names (Role claims).</param>
    /// <param name="permissions">Granular permission names ("permission" claims).</param>
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        Guid? employeeId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);

    /// <summary>Generates a cryptographically random opaque refresh token value.</summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an access token's signature/issuer/audience while IGNORING expiry,
    /// returning its principal — used by the refresh flow to identify the caller.
    /// Returns null when the token is invalid.
    /// </summary>
    /// <param name="accessToken">The (possibly expired) serialized JWT.</param>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}
