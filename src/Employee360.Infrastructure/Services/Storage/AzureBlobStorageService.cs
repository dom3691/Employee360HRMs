using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
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
        var containerClient = GetDocumentsContainer();

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

    /// <inheritdoc />
    public Task<string> GetDownloadUrlAsync(
        string path,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var containerClient = GetDocumentsContainer();
        var blobClient = containerClient.GetBlobClient(path);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Blob client cannot generate SAS URLs. Ensure AzureBlobStorage:ConnectionString includes account key credentials.");
        }

        var lifetime = expiry ?? TimeSpan.FromHours(1);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerClient.Name,
            BlobName = path,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("Generated SAS URL for blob {Path} (expires in {Lifetime})", path, lifetime);

        return Task.FromResult(sasUri.ToString());
    }

    private BlobContainerClient GetDocumentsContainer()
    {
        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is not configured.");
        }

        return new BlobContainerClient(_settings.ConnectionString, _settings.DocumentsContainer);
    }
}
