using FluentValidation;

namespace Employee360.Application.Features.Employees.UploadEmployeeDocument;

/// <summary>
/// Input validation for <see cref="UploadEmployeeDocumentCommand"/> (FR-EMP-007:
/// PDF/JPG/PNG only, max 10 MB).
/// </summary>
public sealed class UploadEmployeeDocumentValidator : AbstractValidator<UploadEmployeeDocumentCommand>
{
    /// <summary>Maximum allowed file size in bytes (10 MB).</summary>
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedContentTypes =
        ["application/pdf", "image/jpeg", "image/png"];

    public UploadEmployeeDocumentValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty().WithMessage("Employee id is required.");

        RuleFor(c => c.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name => AllowedExtensions.Contains(
                Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Only PDF, JPG, and PNG files are allowed.");

        RuleFor(c => c.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct.ToLowerInvariant()))
            .WithMessage("Content type must be application/pdf, image/jpeg, or image/png.");

        RuleFor(c => c.Content)
            .NotEmpty().WithMessage("File content is required.")
            .Must(content => content.LongLength <= MaxSizeBytes)
            .WithMessage("File size must not exceed 10 MB.");

        RuleFor(c => c.Category)
            .IsInEnum().WithMessage("Invalid document category.");

        RuleFor(c => c.ExpiryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(c => c.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be in the future.");
    }
}
