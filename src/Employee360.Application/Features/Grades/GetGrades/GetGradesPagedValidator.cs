using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Grades.GetGrades;

/// <summary>Input validation for <see cref="GetGradesPagedQuery"/>.</summary>
public sealed class GetGradesPagedValidator : AbstractValidator<GetGradesPagedQuery>
{
    public GetGradesPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}
