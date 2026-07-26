using FluentValidation;

namespace Employee360.Application.Features.Departments.CreateDepartment;

/// <summary>Input validation for <see cref="CreateDepartmentCommand"/>.</summary>
public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(128);

        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("Department code is required.")
            .MaximumLength(16)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Department code may only contain letters, numbers, and hyphens.");
    }
}
