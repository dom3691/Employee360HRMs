using FluentValidation;

namespace Employee360.Application.Features.Auth.ForgotPassword;

/// <summary>Input validation for <see cref="ForgotPasswordCommand"/>.</summary>
public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }
}
