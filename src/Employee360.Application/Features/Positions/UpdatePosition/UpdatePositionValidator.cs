using FluentValidation;

namespace Employee360.Application.Features.Positions.UpdatePosition;

/// <summary>Input validation for <see cref="UpdatePositionCommand"/>.</summary>
public sealed class UpdatePositionValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionValidator()
    {
        RuleFor(c => c.PositionId)
            .NotEmpty().WithMessage("Position id is required.");

        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("Position title is required.")
            .MaximumLength(128);

        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("Position code is required.")
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Position code may only contain letters, numbers, and hyphens.");
    }
}
