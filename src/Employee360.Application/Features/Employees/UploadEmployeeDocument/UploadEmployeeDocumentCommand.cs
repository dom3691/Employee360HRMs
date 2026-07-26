using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Employees.UploadEmployeeDocument;

/// <summary>
/// Uploads an employee document to blob storage (FR-EMP-007:
/// PDF/JPG/PNG, max 10 MB, category-tagged, optional expiry).
/// </summary>
public sealed record UploadEmployeeDocumentCommand(
    Guid EmployeeId,
    string FileName,
    string ContentType,
    byte[] Content,
    DocumentCategory Category,
    DateOnly? ExpiryDate) : IRequest<Result<Guid>>;
