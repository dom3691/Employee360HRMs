using System.Security.Cryptography;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Application.Features.Auth.ForgotPassword;

/// <summary>
/// Handles <see cref="ForgotPasswordCommand"/>: issues a 1-hour reset token and
/// emails it to the account. Returns success even for unknown emails to avoid
/// account enumeration (security posture, NFR-SEC-005).
/// </summary>
public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    /// <summary>Reset token lifetime in minutes (FR-AUTH-004: 1 hour).</summary>
    public const int TokenLifetimeMinutes = 60;

    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ForgotPasswordHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower() && u.IsActive,
                cancellationToken);

        if (user is null)
        {
            // Do not reveal whether the account exists.
            return Result.Success();
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAtUtc = utcNow,
            ExpiresAtUtc = utcNow.AddMinutes(TokenLifetimeMinutes),
        });

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendAsync(
                user.Email,
                "Employee360 Password Reset Request",
                $"""
                <p>We received a request to reset your Employee360 password.</p>
                <p>Your reset code is: <strong>{token}</strong></p>
                <p>This code expires in {TokenLifetimeMinutes} minutes. If you did not request a reset, ignore this email.</p>
                """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Email delivery problems must not reveal or block the flow; the token
            // remains valid and support can resend.
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.Id);
        }

        return Result.Success();
    }
}
