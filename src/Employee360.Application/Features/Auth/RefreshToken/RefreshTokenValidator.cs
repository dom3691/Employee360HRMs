using FluentValidation;

namespace Employee360.Application.Features.Auth.RefreshToken;

/// <summary>Input validation for <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(c => c.AccessToken)
            .NotEmpty().WithMessage("Access token is required.");

        RuleFor(c => c.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
