using FluentValidation;

namespace Employee360.Application.Common.Validation;

/// <summary>
/// Reusable password policy rule (FR-AUTH-002): minimum 8 characters with at least
/// one uppercase letter, one lowercase letter, one number, and one special character.
/// </summary>
public static class PasswordRuleBuilderExtensions
{
    /// <summary>Applies the corporate password policy to a string property.</summary>
    /// <typeparam name="T">The command type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the password property.</param>
    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}
