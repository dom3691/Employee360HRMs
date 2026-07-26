using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Auth.ResetPassword;

/// <summary>Completes a password reset using an emailed token (FR-AUTH-004).</summary>
/// <param name="Email">The account email.</param>
/// <param name="Token">The reset token from the email.</param>
/// <param name="NewPassword">The new password (must satisfy the password policy).</param>
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result>;
