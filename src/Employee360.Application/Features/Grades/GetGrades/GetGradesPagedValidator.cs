using FluentValidation;

namespace Employee360.Application.Features.Grades.GetGrades;

/// <summary>Input validation for <see cref="GetGradesPagedQuery"/>.</summary>
public sealed class GetGradesPagedValidator : AbstractValidator<GetGradesPagedQuery>
{
    public GetGradesPagedValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Page size must be between 1 and 200.");
    }
}
