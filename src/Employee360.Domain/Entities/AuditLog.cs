using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Immutable audit trail row recording every create/update/delete on tracked
/// entities (PRD NFR-AUD-001). Written automatically by the persistence layer's
/// SaveChanges interceptor; never modified or deleted by application code.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>CLR name of the entity type that changed (e.g. "Employee").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primary key of the changed entity.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>The change action: Created, Updated, or Deleted.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the changed columns' previous values; null for creates.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the changed columns' new values; null for deletes.</summary>
    public string? NewValues { get; set; }

    /// <summary>Id of the user who made the change, or null for system/background jobs.</summary>
    public Guid? UserId { get; set; }

    /// <summary>UTC timestamp of the change.</summary>
    public DateTime Timestamp { get; set; }
}
