using FluentValidation;

namespace Employee360.Application.Features.Employees.GetEmployeesPaged;

/// <summary>Input validation for <see cref="GetEmployeesPagedQuery"/>.</summary>
public sealed class GetEmployeesPagedValidator : AbstractValidator<GetEmployeesPagedQuery>
{
    private static readonly string[] SortFields = ["name", "code", "joindate"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public GetEmployeesPagedValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(q => q.SortBy)
            .Must(s => SortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, code, joinDate.");

        RuleFor(q => q.SortDirection)
            .Must(s => SortDirections.Contains(s.ToLowerInvariant()))
            .WithMessage("SortDirection must be asc or desc.");
    }
}
