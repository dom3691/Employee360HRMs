namespace Employee360.Infrastructure.Services.Email;

/// <summary>Strongly-typed binding of the "Email:AzureCommunicationServices" section.</summary>
public sealed class AzureCommunicationEmailSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email:AzureCommunicationServices";

    /// <summary>Azure Communication Services connection string.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Verified sender address in ACS.</summary>
    public string FromAddress { get; init; } = string.Empty;

    /// <summary>Sender display name.</summary>
    public string FromDisplayName { get; init; } = "Employee360 HRMS";
}
