using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Auth.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>: verifies credentials, enforces the lockout
/// policy (5 failed attempts → 15-minute lock, FR-AUTH-003), and issues the
/// access + refresh token pair.
/// </summary>
public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    /// <summary>Failed attempts that trigger a lockout.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Lockout duration in minutes.</summary>
    public const int LockoutMinutes = 15;

    private const string InvalidCredentials = "Invalid email or password.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var email = request.Email.Trim();

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>("This account has been deactivated. Contact HR.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > utcNow)
        {
            return Result.Failure<LoginResponse>(
                "Account is temporarily locked due to repeated failed login attempts. Try again later.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = utcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;

                await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure<LoginResponse>(
                    $"Account locked for {LockoutMinutes} minutes after {MaxFailedAttempts} failed login attempts.");
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        // Successful login: clear lockout counters and stamp the visit.
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAt = utcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.EmployeeId, roles, permissions);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken.Token,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            CreatedAtUtc = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            user.Email,
            roles));
    }
}
