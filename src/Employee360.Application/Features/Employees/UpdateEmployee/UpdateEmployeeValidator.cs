using FluentValidation;

namespace Employee360.Application.Features.Employees.UpdateEmployee;

/// <summary>Input validation for <see cref="UpdateEmployeeCommand"/>.</summary>
public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty().WithMessage("Employee id is required.");

        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(c => c.Nin)
            .Matches("^[0-9]{11}$")
            .When(c => !string.IsNullOrEmpty(c.Nin))
            .WithMessage("NIN must be exactly 11 digits.");

        When(c => c.BankAccount is not null, () =>
        {
            RuleFor(c => c.BankAccount!.BankName)
                .NotEmpty().WithMessage("Bank name is required.");

            RuleFor(c => c.BankAccount!.AccountNumber)
                .Matches("^[0-9]{10}$")
                .WithMessage("Account number must be a 10-digit NUBAN number.");

            RuleFor(c => c.BankAccount!.AccountName)
                .NotEmpty().WithMessage("Account name is required.");
        });
    }
}
