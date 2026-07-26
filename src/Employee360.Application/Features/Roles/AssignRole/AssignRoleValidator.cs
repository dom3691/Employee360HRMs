using FluentValidation;

namespace Employee360.Application.Features.Roles.AssignRole;

/// <summary>Input validation for <see cref="AssignRoleCommand"/>.</summary>
public sealed class AssignRoleValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(c => c.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}
