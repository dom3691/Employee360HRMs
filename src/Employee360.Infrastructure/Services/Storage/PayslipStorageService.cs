using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Employee360.Infrastructure.Services.Storage;

/// <summary>
/// Azure Blob Storage for payslip PDFs (dedicated payslips container).
/// </summary>
public sealed class PayslipStorageService : IPayslipStorageService
{
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<PayslipStorageService> _logger;

    public PayslipStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<PayslipStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(path);
        await blob.UploadAsync(
            new BinaryData(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" },
            },
            cancellationToken);

        _logger.LogInformation("Uploaded payslip {Path} ({Size} bytes)", path, content.Length);
        return path;
    }

    public Task<string> GetDownloadUrlAsync(
        string path,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var container = GetContainer();
        var blob = container.GetBlobClient(path);

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Blob client cannot generate SAS URLs. Ensure AzureBlobStorage:ConnectionString includes account key credentials.");
        }

        var lifetime = expiry ?? TimeSpan.FromHours(1);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = path,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return Task.FromResult(blob.GenerateSasUri(sasBuilder).ToString());
    }

    private BlobContainerClient GetContainer()
    {
        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");
        }

        return new BlobContainerClient(_settings.ConnectionString, _settings.PayslipsContainer);
    }
}
