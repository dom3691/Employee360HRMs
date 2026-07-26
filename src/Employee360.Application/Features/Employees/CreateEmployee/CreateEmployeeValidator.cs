using FluentValidation;

namespace Employee360.Application.Features.Employees.CreateEmployee;

/// <summary>Input validation for <see cref="CreateEmployeeCommand"/> (FR-EMP-002/003).</summary>
public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(c => c.MiddleName)
            .MaximumLength(100);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256);

        RuleFor(c => c.PhoneNumber)
            .MaximumLength(32);

        RuleFor(c => c.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)))
            .When(c => c.DateOfBirth.HasValue)
            .WithMessage("Employee must be at least 16 years old.");

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

        RuleForEach(c => c.Contacts).ChildRules(contact =>
        {
            contact.RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Contact full name is required.");
            contact.RuleFor(x => x.Relationship)
                .NotEmpty().WithMessage("Contact relationship is required.");
            contact.RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Contact phone number is required.");
        });
    }
}
