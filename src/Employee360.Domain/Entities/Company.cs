using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Organization tenant profile (PRD FR-ADM-001). Single-tenant in Phase 1;
/// one row seeded at startup with <see cref="DefaultId"/>.
/// </summary>
public class Company : AuditableEntity
{
    /// <summary>Well-known id for the singleton company row.</summary>
    public static readonly Guid DefaultId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Legal / trading name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>CAC Registration Certificate number.</summary>
    public string? RCNumber { get; set; }

    /// <summary>Tax Identification Number (FIRS).</summary>
    public string? TIN { get; set; }

    /// <summary>Registered or head-office address.</summary>
    public string? Address { get; set; }

    /// <summary>URL to the company logo (blob storage or CDN).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>ISO 4217 currency code; default NGN for Nigerian deployments.</summary>
    public string DefaultCurrency { get; set; } = "NGN";

    /// <summary>Trading / brand name shown in UI.</summary>
    public string? TradingName { get; set; }

    /// <summary>Legal entity name.</summary>
    public string? LegalName { get; set; }

    /// <summary>Industry sector.</summary>
    public string? Industry { get; set; }

    /// <summary>Company website URL.</summary>
    public string? Website { get; set; }

    /// <summary>Primary contact phone.</summary>
    public string? Phone { get; set; }

    /// <summary>Primary contact email.</summary>
    public string? Email { get; set; }

    /// <summary>Street address line.</summary>
    public string? StreetAddress { get; set; }

    /// <summary>City.</summary>
    public string? City { get; set; }

    /// <summary>State / region.</summary>
    public string? State { get; set; }

    /// <summary>Country.</summary>
    public string? Country { get; set; }

    /// <summary>Serialized working calendar settings (JSON).</summary>
    public string? WorkingCalendarJson { get; set; }

    /// <summary>Serialized SMTP / email sender settings (JSON).</summary>
    public string? EmailConfigurationJson { get; set; }
}
