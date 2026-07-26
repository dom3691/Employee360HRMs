using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Auth.ResetPassword;

/// <summary>Input validation for <see cref="ResetPasswordCommand"/> (FR-AUTH-002 policy).</summary>
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(c => c.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(c => c.NewPassword).MustBeStrongPassword();
    }
}
