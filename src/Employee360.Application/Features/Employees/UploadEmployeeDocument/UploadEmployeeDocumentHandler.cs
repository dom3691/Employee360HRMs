using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.UploadEmployeeDocument;

/// <summary>
/// Handles <see cref="UploadEmployeeDocumentCommand"/>: stores the file in blob
/// storage under a collision-free path and records the document metadata.
/// </summary>
public sealed class UploadEmployeeDocumentHandler
    : IRequestHandler<UploadEmployeeDocumentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public UploadEmployeeDocumentHandler(
        IApplicationDbContext context,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        UploadEmployeeDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return Result.Failure<Guid>("Employee not found.");
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var storagePath = $"employees/{request.EmployeeId}/{Guid.NewGuid():N}{extension}";

        var storedPath = await _fileStorageService.UploadAsync(
            storagePath, request.Content, request.ContentType, cancellationToken);

        var document = new EmployeeDocument
        {
            EmployeeId = request.EmployeeId,
            Category = request.Category,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.Content.LongLength,
            StoragePath = storedPath,
            ExpiryDate = request.ExpiryDate,
        };

        _context.EmployeeDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(document.Id);
    }
}
