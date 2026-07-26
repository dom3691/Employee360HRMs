using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Auth.ChangePassword;

/// <summary>Changes the authenticated user's password (self-service).</summary>
/// <param name="CurrentPassword">The current password, re-verified before the change.</param>
/// <param name="NewPassword">The new password (must satisfy the password policy).</param>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
    : IRequest<Result>;
