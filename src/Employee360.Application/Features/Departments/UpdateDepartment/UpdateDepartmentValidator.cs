using FluentValidation;

namespace Employee360.Application.Features.Departments.UpdateDepartment;

/// <summary>Input validation for <see cref="UpdateDepartmentCommand"/>.</summary>
public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(c => c.DepartmentId)
            .NotEmpty().WithMessage("Department id is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(128);

        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("Department code is required.")
            .MaximumLength(16)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Department code may only contain letters, numbers, and hyphens.");

        RuleFor(c => c.ParentDepartmentId)
            .NotEqual(c => c.DepartmentId)
            .When(c => c.ParentDepartmentId.HasValue)
            .WithMessage("A department cannot be its own parent.");
    }
}
