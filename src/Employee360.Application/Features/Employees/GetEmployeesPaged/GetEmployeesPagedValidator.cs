using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Employees.GetEmployeesPaged;

/// <summary>Input validation for <see cref="GetEmployeesPagedQuery"/>.</summary>
public sealed class GetEmployeesPagedValidator : AbstractValidator<GetEmployeesPagedQuery>
{
    private static readonly string[] SortFields = ["name", "code", "joindate"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public GetEmployeesPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();

        RuleFor(q => q.SortBy)
            .Must(s => SortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, code, joinDate.");

        RuleFor(q => q.SortDirection)
            .Must(s => SortDirections.Contains(s.ToLowerInvariant()))
            .WithMessage("SortDirection must be asc or desc.");
    }
}
