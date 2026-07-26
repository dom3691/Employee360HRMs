using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Job title / position (PRD FR-EMP-010). Minimal form for employee assignment;
/// position CRUD slices arrive with the org-structure batch.
/// </summary>
public class Position : AuditableEntity, ISoftDelete
{
    /// <summary>Position title, e.g. "Senior Accountant".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Short unique code, e.g. "SR-ACC".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Department this position belongs to, when scoped.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Navigation to the owning department.</summary>
    public Department? Department { get; set; }

    /// <summary>Employees holding this position.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
