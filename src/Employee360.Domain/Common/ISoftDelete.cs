namespace Employee360.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable. Deletion sets <see cref="IsDeleted"/> instead of
/// removing the row; EF Core global query filters exclude soft-deleted rows by default.
/// Employee and HR records are never hard-deleted (audit/retention requirement,
/// PRD NFR-RET-002).
/// </summary>
public interface ISoftDelete
{
    /// <summary>True when the record has been soft-deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the record was soft-deleted, or null when active.</summary>
    DateTime? DeletedAt { get; set; }
}
