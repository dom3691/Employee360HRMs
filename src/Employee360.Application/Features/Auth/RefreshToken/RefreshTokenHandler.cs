using System.Security.Claims;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Auth.Login;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Auth.RefreshToken;

/// <summary>
/// Handles <see cref="RefreshTokenCommand"/>: validates the expired access token's
/// signature, verifies the refresh token belongs to the same user and is active,
/// then rotates it (old token revoked and linked to its successor).
/// </summary>
public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private const string InvalidToken = "Invalid or expired refresh token.";

    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);

        if (principal is null ||
            !Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Result.Failure<LoginResponse>("Invalid access token.");
        }

        var utcNow = _dateTimeProvider.UtcNow;

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken is null ||
            storedToken.UserId != userId ||
            storedToken.RevokedAtUtc is not null ||
            storedToken.ExpiresAtUtc <= utcNow)
        {
            return Result.Failure<LoginResponse>(InvalidToken);
        }

        var user = storedToken.User;

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>("This account has been deactivated. Contact HR.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.EmployeeId, roles, permissions);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Rotation: revoke the consumed token and link it to its successor.
        storedToken.RevokedAtUtc = utcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken.Token,
            ExpiresAtUtc = newRefreshToken.ExpiresAtUtc,
            CreatedAtUtc = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            newRefreshToken.Token,
            newRefreshToken.ExpiresAtUtc,
            user.Email,
            roles));
    }
}
