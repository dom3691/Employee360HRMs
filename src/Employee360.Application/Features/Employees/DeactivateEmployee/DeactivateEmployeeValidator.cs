using Employee360.Domain.Enums;
using FluentValidation;

namespace Employee360.Application.Features.Employees.DeactivateEmployee;

/// <summary>Input validation for <see cref="DeactivateEmployeeCommand"/>.</summary>
public sealed class DeactivateEmployeeValidator : AbstractValidator<DeactivateEmployeeCommand>
{
    private static readonly EmployeeStatus[] AllowedStatuses =
    [
        EmployeeStatus.Suspended,
        EmployeeStatus.Resigned,
        EmployeeStatus.Terminated,
    ];

    public DeactivateEmployeeValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty().WithMessage("Employee id is required.");

        RuleFor(c => c.NewStatus)
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be Suspended, Resigned, or Terminated.");

        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("A reason is required for deactivation.")
            .MaximumLength(512);
    }
}
