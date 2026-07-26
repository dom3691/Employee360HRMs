using FluentValidation;

namespace Employee360.Application.Features.Leave.AdjustLeaveBalance;

/// <summary>Input validation for <see cref="AdjustLeaveBalanceCommand"/>.</summary>
public sealed class AdjustLeaveBalanceValidator : AbstractValidator<AdjustLeaveBalanceCommand>
{
    public AdjustLeaveBalanceValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty().WithMessage("Employee id is required.");

        RuleFor(c => c.LeaveTypeId)
            .NotEmpty().WithMessage("Leave type id is required.");

        RuleFor(c => c.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year is out of range.");

        RuleFor(c => c.AdjustmentDays)
            .NotEqual(0).WithMessage("Adjustment must be non-zero.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("A reason is required for balance adjustments.")
            .MaximumLength(512);
    }
}
