using FluentValidation;

namespace Employee360.Application.Features.Leave.CreateLeaveType;

/// <summary>Input validation for <see cref="CreateLeaveTypeCommand"/>.</summary>
public sealed class CreateLeaveTypeValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Leave type name is required.")
            .MaximumLength(64);

        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("Leave type code is required.")
            .MaximumLength(16)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Code may only contain letters, numbers, and hyphens.");

        RuleFor(c => c.Color)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(c => !string.IsNullOrEmpty(c.Color))
            .WithMessage("Color must be a hex value like #2E7D32.");
    }
}
