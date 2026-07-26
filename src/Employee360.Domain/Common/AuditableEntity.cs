namespace Employee360.Domain.Common;

/// <summary>
/// Base class for entities that carry audit columns. The columns are populated
/// automatically by the persistence layer's SaveChanges interceptor — application
/// code must never set them manually.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>Identifier of the user who created the record.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Identifier of the user who last modified the record.</summary>
    public string? ModifiedBy { get; set; }

    /// <summary>UTC timestamp of the last modification, or null if never modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}
