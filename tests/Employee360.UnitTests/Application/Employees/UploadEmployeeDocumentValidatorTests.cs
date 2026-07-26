using Employee360.Application.Features.Employees.UploadEmployeeDocument;
using Employee360.Domain.Enums;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Employees;

/// <summary>
/// Tests for document upload validation (FR-EMP-007: PDF/JPG/PNG, max 10 MB).
/// </summary>
public class UploadEmployeeDocumentValidatorTests
{
    private readonly UploadEmployeeDocumentValidator _validator = new();

    private static UploadEmployeeDocumentCommand NewCommand(
        string fileName = "contract.pdf",
        string contentType = "application/pdf",
        int sizeBytes = 1024,
        DocumentCategory category = DocumentCategory.Contract)
        => new(Guid.NewGuid(), fileName, contentType, new byte[sizeBytes], category, null);

    [Fact]
    public void ValidPdf_ShouldPass()
    {
        _validator.Validate(NewCommand()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("scan.png", "image/png")]
    public void ValidImages_ShouldPass(string fileName, string contentType)
    {
        _validator.Validate(NewCommand(fileName, contentType)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("malware.exe", "application/octet-stream")]
    [InlineData("sheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("archive.zip", "application/zip")]
    public void DisallowedTypes_ShouldFail(string fileName, string contentType)
    {
        var result = _validator.Validate(NewCommand(fileName, contentType));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PDF, JPG, and PNG"));
    }

    [Fact]
    public void OversizeFile_ShouldFail()
    {
        var command = NewCommand(sizeBytes: (int)UploadEmployeeDocumentValidator.MaxSizeBytes + 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("10 MB"));
    }

    [Fact]
    public void FileAtExactly10Mb_ShouldPass()
    {
        var command = NewCommand(sizeBytes: (int)UploadEmployeeDocumentValidator.MaxSizeBytes);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyContent_ShouldFail()
    {
        var result = _validator.Validate(NewCommand(sizeBytes: 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MismatchedContentType_ShouldFail()
    {
        // PDF extension with spreadsheet MIME type.
        var result = _validator.Validate(NewCommand("contract.pdf", "application/vnd.ms-excel"));

        result.IsValid.Should().BeFalse();
    }
}
