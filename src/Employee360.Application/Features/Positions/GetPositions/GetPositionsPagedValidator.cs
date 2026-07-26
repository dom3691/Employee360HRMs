using FluentValidation;

namespace Employee360.Application.Features.Positions.GetPositions;

/// <summary>Input validation for <see cref="GetPositionsPagedQuery"/>.</summary>
public sealed class GetPositionsPagedValidator : AbstractValidator<GetPositionsPagedQuery>
{
    public GetPositionsPagedValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
