using FluentValidation;

namespace Employee360.Application.Features.Leave.ApplyLeave;

/// <summary>Input validation for <see cref="ApplyLeaveCommand"/>.</summary>
public sealed class ApplyLeaveValidator : AbstractValidator<ApplyLeaveCommand>
{
    public ApplyLeaveValidator()
    {
        RuleFor(c => c.LeaveTypeId)
            .NotEmpty().WithMessage("Leave type is required.");

        RuleFor(c => c.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(1024);
    }
}
