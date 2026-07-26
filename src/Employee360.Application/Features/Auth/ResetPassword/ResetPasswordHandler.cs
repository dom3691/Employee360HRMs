using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Auth.ResetPassword;

/// <summary>
/// Handles <see cref="ResetPasswordCommand"/>: consumes a valid, unexpired reset
/// token, sets the new password hash, clears lockout state, and revokes all active
/// refresh tokens (defence in depth after credential change).
/// </summary>
public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private const string InvalidToken = "Invalid or expired reset token.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResetPasswordHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var email = request.Email.Trim();

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.Token == request.Token &&
                     t.User.Email.ToLower() == email.ToLower(),
                cancellationToken);

        if (resetToken is null ||
            resetToken.UsedAtUtc is not null ||
            resetToken.ExpiresAtUtc <= utcNow)
        {
            return Result.Failure(InvalidToken);
        }

        var user = resetToken.User;

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        resetToken.UsedAtUtc = utcNow;

        // Credential change invalidates every outstanding session.
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
