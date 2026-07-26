using FluentValidation;

namespace Employee360.Application.Features.Leave.ReturnLeaveForRevision;

/// <summary>Input validation for <see cref="ReturnLeaveForRevisionCommand"/>.</summary>
public sealed class ReturnLeaveForRevisionValidator : AbstractValidator<ReturnLeaveForRevisionCommand>
{
    public ReturnLeaveForRevisionValidator()
    {
        RuleFor(c => c.RequestId)
            .NotEmpty().WithMessage("Request id is required.");

        RuleFor(c => c.Comments)
            .NotEmpty().WithMessage("A comment is required when returning a request for revision.")
            .MaximumLength(1024);
    }
}
