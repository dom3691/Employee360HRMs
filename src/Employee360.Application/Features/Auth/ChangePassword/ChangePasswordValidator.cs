using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Auth.ChangePassword;

/// <summary>Input validation for <see cref="ChangePasswordCommand"/> (FR-AUTH-002 policy).</summary>
public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(c => c.NewPassword).MustBeStrongPassword();

        RuleFor(c => c.NewPassword)
            .NotEqual(c => c.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}
