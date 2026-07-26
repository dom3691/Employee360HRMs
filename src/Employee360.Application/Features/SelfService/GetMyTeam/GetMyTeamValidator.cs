using FluentValidation;

namespace Employee360.Application.Features.SelfService.GetMyTeam;

/// <summary>Input validation for <see cref="GetMyTeamQuery"/>.</summary>
public sealed class GetMyTeamValidator : AbstractValidator<GetMyTeamQuery>
{
    public GetMyTeamValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
