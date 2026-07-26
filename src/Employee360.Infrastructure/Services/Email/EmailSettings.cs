namespace Employee360.Infrastructure.Services.Email;

/// <summary>Strongly-typed binding of the "Email:Smtp" configuration section.</summary>
public sealed class EmailSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email:Smtp";

    /// <summary>SMTP server host.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>SMTP server port (587 for STARTTLS).</summary>
    public int Port { get; init; } = 587;

    /// <summary>True to negotiate TLS.</summary>
    public bool UseSsl { get; init; } = true;

    /// <summary>SMTP username (empty for anonymous relay).</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>SMTP password.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Sender address for outbound mail.</summary>
    public string FromAddress { get; init; } = string.Empty;

    /// <summary>Sender display name.</summary>
    public string FromDisplayName { get; init; } = "Employee360 HRMS";
}
