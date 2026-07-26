using FluentValidation;

namespace Employee360.Application.Features.Employees.SubmitProfileChangeRequest;

/// <summary>
/// Input validation for <see cref="SubmitProfileChangeRequestCommand"/>: only
/// whitelisted self-service fields may be requested (FR-EMP-011).
/// </summary>
public sealed class SubmitProfileChangeRequestValidator
    : AbstractValidator<SubmitProfileChangeRequestCommand>
{
    /// <summary>Fields an employee may request to change without direct HR edit.</summary>
    public static readonly string[] AllowedFields = ["PhoneNumber", "Address"];

    public SubmitProfileChangeRequestValidator()
    {
        RuleFor(c => c.Changes)
            .NotEmpty().WithMessage("At least one change is required.");

        RuleForEach(c => c.Changes)
            .Must(kvp => AllowedFields.Contains(kvp.Key))
            .WithMessage((_, kvp) =>
                $"Field '{kvp.Key}' cannot be changed via self-service. Allowed: {string.Join(", ", AllowedFields)}.");

        RuleForEach(c => c.Changes)
            .Must(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .WithMessage("Requested values must not be empty.");
    }
}
