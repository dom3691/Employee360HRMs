using FluentValidation;

namespace Employee360.Application.Features.Employees.BulkImportEmployees;

/// <summary>Input validation for <see cref="BulkImportEmployeesCommand"/>.</summary>
public sealed class BulkImportEmployeesValidator : AbstractValidator<BulkImportEmployeesCommand>
{
    /// <summary>Maximum CSV size (2 MB ≈ tens of thousands of rows).</summary>
    public const long MaxSizeBytes = 2 * 1024 * 1024;

    public BulkImportEmployeesValidator()
    {
        RuleFor(c => c.CsvContent)
            .NotEmpty().WithMessage("CSV content is required.")
            .Must(content => content.LongLength <= MaxSizeBytes)
            .WithMessage("CSV file must not exceed 2 MB.");
    }
}
