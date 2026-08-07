using FluentValidation;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Input validation for <see cref="UpdateGradeCommand"/>.</summary>
public sealed class UpdateGradeValidator : AbstractValidator<UpdateGradeCommand>
{
    public UpdateGradeValidator()
    {
        RuleFor(c => c.GradeId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Code).NotEmpty().MaximumLength(16);
        RuleFor(c => c.LevelRank).NotEmpty().MaximumLength(32);
        RuleFor(c => c.SalaryMin).GreaterThanOrEqualTo(0);
        RuleFor(c => c.SalaryMax).GreaterThanOrEqualTo(c => c.SalaryMin);
    }
}
