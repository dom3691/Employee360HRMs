using FluentValidation;

namespace Employee360.Application.Features.Roles.UpdateRolePermissions;

/// <summary>Input validation for <see cref="UpdateRolePermissionsCommand"/>.</summary>
public sealed class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(c => c.RoleId)
            .NotEmpty().WithMessage("Role id is required.");

        RuleFor(c => c.PermissionNames)
            .NotNull().WithMessage("Permission list is required (empty list removes all permissions).");

        RuleForEach(c => c.PermissionNames)
            .NotEmpty().WithMessage("Permission names must not be empty.");
    }
}
