using FluentValidation;

namespace Employee360.Application.Features.Employees.ReviewProfileChangeRequest;

/// <summary>Input validation for <see cref="ReviewProfileChangeRequestCommand"/>.</summary>
public sealed class ReviewProfileChangeRequestValidator
    : AbstractValidator<ReviewProfileChangeRequestCommand>
{
    public ReviewProfileChangeRequestValidator()
    {
        RuleFor(c => c.RequestId)
            .NotEmpty().WithMessage("Request id is required.");

        RuleFor(c => c.Comment)
            .NotEmpty()
            .When(c => !c.Approve)
            .WithMessage("A comment is required when rejecting a request.");

        RuleFor(c => c.Comment)
            .MaximumLength(512);
    }
}
