using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Positions.GetPositions;

/// <summary>Input validation for <see cref="GetPositionsPagedQuery"/>.</summary>
public sealed class GetPositionsPagedValidator : AbstractValidator<GetPositionsPagedQuery>
{
    public GetPositionsPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}
