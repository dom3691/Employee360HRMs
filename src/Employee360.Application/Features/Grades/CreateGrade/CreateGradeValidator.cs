using FluentValidation;

namespace Employee360.Application.Features.Grades.CreateGrade;

/// <summary>Input validation for <see cref="CreateGradeCommand"/>.</summary>
public sealed class CreateGradeValidator : AbstractValidator<CreateGradeCommand>
{
    public CreateGradeValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("Grade title is required.")
            .MaximumLength(64);

        RuleFor(c => c.Code)
            .NotEmpty().MaximumLength(16);

        RuleFor(c => c.LevelRank)
            .NotEmpty().MaximumLength(32);

        RuleFor(c => c.SalaryMin)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.SalaryMax)
            .GreaterThanOrEqualTo(c => c.SalaryMin);
    }
}
