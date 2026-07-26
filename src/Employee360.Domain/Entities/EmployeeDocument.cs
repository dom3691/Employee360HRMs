using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Employee document metadata (PRD FR-EMP-007). File bytes live in Azure Blob
/// Storage; this row records category, location, and expiry.
/// </summary>
public class EmployeeDocument : AuditableEntity, ISoftDelete
{
    /// <summary>Owning employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the owning employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Document category (FR-EMP-007).</summary>
    public DocumentCategory Category { get; set; }

    /// <summary>Original file name as uploaded.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME content type (application/pdf, image/jpeg, image/png).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes (max 10 MB).</summary>
    public long SizeBytes { get; set; }

    /// <summary>Blob storage path.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Expiry date for time-limited documents (e.g. work permits).</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
