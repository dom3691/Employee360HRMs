using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Auth.ForgotPassword;

/// <summary>
/// Initiates a password reset: emails a single-use token valid for 1 hour
/// (FR-AUTH-004). Always succeeds outwardly so account existence is not revealed.
/// </summary>
/// <param name="Email">The account email address.</param>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
