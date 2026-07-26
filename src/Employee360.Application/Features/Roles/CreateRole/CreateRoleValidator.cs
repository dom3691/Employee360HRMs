using FluentValidation;

namespace Employee360.Application.Features.Roles.CreateRole;

/// <summary>Input validation for <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(64).WithMessage("Role name must not exceed 64 characters.")
            .Matches("^[A-Za-z][A-Za-z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, hyphens, or underscores.");

        RuleFor(c => c.Description)
            .MaximumLength(512).WithMessage("Description must not exceed 512 characters.");
    }
}
