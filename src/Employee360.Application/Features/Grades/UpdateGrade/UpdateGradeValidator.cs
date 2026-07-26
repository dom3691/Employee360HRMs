using FluentValidation;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Input validation for <see cref="UpdateGradeCommand"/>.</summary>
public sealed class UpdateGradeValidator : AbstractValidator<UpdateGradeCommand>
{
    public UpdateGradeValidator()
    {
        RuleFor(c => c.GradeId)
            .NotEmpty().WithMessage("Grade id is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Grade name is required.")
            .MaximumLength(64);

        RuleFor(c => c.Level)
            .GreaterThanOrEqualTo(1).WithMessage("Level must be 1 or greater.");

        RuleFor(c => c.MinSalary)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum salary cannot be negative.");

        RuleFor(c => c.MaxSalary)
            .GreaterThanOrEqualTo(c => c.MinSalary)
            .WithMessage("Maximum salary must be greater than or equal to minimum salary.");
    }
}
