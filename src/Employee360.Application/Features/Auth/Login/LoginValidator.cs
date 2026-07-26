using FluentValidation;

namespace Employee360.Application.Features.Auth.Login;

/// <summary>Input validation for <see cref="LoginCommand"/>.</summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
