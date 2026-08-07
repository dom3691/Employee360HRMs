using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.SelfService.GetMyTeam;

/// <summary>Input validation for <see cref="GetMyTeamQuery"/>.</summary>
public sealed class GetMyTeamValidator : AbstractValidator<GetMyTeamQuery>
{
    public GetMyTeamValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}
