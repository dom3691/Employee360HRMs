using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Employee360.Infrastructure.Identity;

/// <summary>
/// JWT implementation of <see cref="IJwtTokenService"/>. Access tokens are
/// HMAC-SHA256-signed and embed identity, role, and permission claims; refresh
/// tokens are 64 bytes of cryptographic randomness (persisted via RefreshToken).
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    /// <summary>Claim type carrying granular permissions.</summary>
    public const string PermissionClaimType = "permission";

    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(IOptions<JwtSettings> settings, IDateTimeProvider dateTimeProvider)
    {
        _settings = settings.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        Guid? employeeId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var expiresAtUtc = utcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        if (employeeId.HasValue)
        {
            claims.Add(new Claim(CurrentUserService.EmployeeIdClaimType, employeeId.Value.ToString()));
        }

        claims.AddRange(roles.Distinct().Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Distinct().Select(p => new Claim(PermissionClaimType, p)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            ValidateLifetime = false, // expired tokens are acceptable for the refresh flow
        };

        // Default inbound claim mapping restores ClaimTypes.* claim types symmetric
        // with generation (same behavior as the JwtBearer middleware).
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            // Reject tokens signed with an unexpected algorithm.
            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
