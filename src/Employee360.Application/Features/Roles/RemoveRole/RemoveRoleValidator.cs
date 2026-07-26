using FluentValidation;

namespace Employee360.Application.Features.Roles.RemoveRole;

/// <summary>Input validation for <see cref="RemoveRoleCommand"/>.</summary>
public sealed class RemoveRoleValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(c => c.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}
