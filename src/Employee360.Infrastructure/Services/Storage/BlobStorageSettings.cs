namespace Employee360.Infrastructure.Services.Storage;

/// <summary>Strongly-typed binding of the "AzureBlobStorage" configuration section.</summary>
public sealed class BlobStorageSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AzureBlobStorage";

    /// <summary>Azure Storage connection string (Key Vault in production).</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Container for employee documents.</summary>
    public string DocumentsContainer { get; init; } = "employee-documents";

    /// <summary>Container for payslip PDFs (Phase 2).</summary>
    public string PayslipsContainer { get; init; } = "payslips";
}
