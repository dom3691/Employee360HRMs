using FluentValidation;

namespace Employee360.Application.Features.Leave.ConfigureLeavePolicy;

/// <summary>Input validation for <see cref="ConfigureLeavePolicyCommand"/>.</summary>
public sealed class ConfigureLeavePolicyValidator : AbstractValidator<ConfigureLeavePolicyCommand>
{
    public ConfigureLeavePolicyValidator()
    {
        RuleFor(c => c.LeaveTypeId)
            .NotEmpty().WithMessage("Leave type id is required.");

        RuleFor(c => c.AnnualEntitlement)
            .InclusiveBetween(0, 366).WithMessage("Annual entitlement must be between 0 and 366 days.");

        RuleFor(c => c.CarryForwardMax)
            .GreaterThanOrEqualTo(0).WithMessage("Carry-forward cap cannot be negative.");

        RuleFor(c => c.ProbationMonths)
            .InclusiveBetween(0, 24).WithMessage("Probation months must be between 0 and 24.");
    }
}
