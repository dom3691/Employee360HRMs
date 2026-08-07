using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Departments.GetDepartments;

/// <summary>Input validation for <see cref="GetDepartmentsPagedQuery"/>.</summary>
public sealed class GetDepartmentsPagedValidator : AbstractValidator<GetDepartmentsPagedQuery>
{
    public GetDepartmentsPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}
