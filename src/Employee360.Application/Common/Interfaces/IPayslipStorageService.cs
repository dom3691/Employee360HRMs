namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Payslip PDF blob storage (separate Azure container from employee documents).
/// </summary>
public interface IPayslipStorageService
{
    Task<string> UploadAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(
        string path,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
