using FluentValidation;

namespace Employee360.Application.Features.Leave.RejectLeave;

/// <summary>Input validation for <see cref="RejectLeaveCommand"/>.</summary>
public sealed class RejectLeaveValidator : AbstractValidator<RejectLeaveCommand>
{
    public RejectLeaveValidator()
    {
        RuleFor(c => c.RequestId)
            .NotEmpty().WithMessage("Request id is required.");

        RuleFor(c => c.Comments)
            .NotEmpty().WithMessage("A comment is required when rejecting a leave request.")
            .MaximumLength(1024);
    }
}
