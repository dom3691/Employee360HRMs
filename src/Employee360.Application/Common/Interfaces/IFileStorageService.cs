namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Binary file storage abstraction (Azure Blob Storage in Infrastructure).
/// Used for employee documents (FR-EMP-007) and payslip PDFs (Phase 2).
/// </summary>
public interface IFileStorageService
{
    /// <summary>Uploads a file and returns its storage path.</summary>
    /// <param name="path">Relative storage path, e.g. "employees/{id}/{file}".</param>
    /// <param name="content">File bytes.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> UploadAsync(
        string path,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited SAS URL for downloading a blob (documents/payslips).
    /// </summary>
    /// <param name="path">Relative storage path returned from upload.</param>
    /// <param name="expiry">Optional SAS lifetime; defaults to 1 hour.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> GetDownloadUrlAsync(
        string path,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
