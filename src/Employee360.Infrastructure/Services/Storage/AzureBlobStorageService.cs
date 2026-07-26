using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Employee360.Infrastructure.Services.Storage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorageService"/>
/// (PRD Integration: document and payslip storage, encrypted at rest).
/// </summary>
public sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<AzureBlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        string path,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is not configured.");
        }

        var containerClient = new BlobContainerClient(
            _settings.ConnectionString, _settings.DocumentsContainer);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(path);

        await blobClient.UploadAsync(
            new BinaryData(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            cancellationToken);

        _logger.LogInformation("Uploaded blob {Path} ({Size} bytes)", path, content.Length);

        return path;
    }
}
