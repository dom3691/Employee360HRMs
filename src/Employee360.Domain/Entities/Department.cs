using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Organizational unit with optional parent hierarchy (PRD FR-EMP-009).
/// Introduced in minimal form for employee assignment; department CRUD slices
/// arrive with the org-structure batch.
/// </summary>
public class Department : AuditableEntity, ISoftDelete
{
    /// <summary>Department name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique code, e.g. "ENG".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Parent department, or null for top-level.</summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>Navigation to the parent department.</summary>
    public Department? ParentDepartment { get; set; }

    /// <summary>Department head (FR-EMP-009), or null when unassigned.</summary>
    public Guid? HeadEmployeeId { get; set; }

    /// <summary>Navigation to the department head.</summary>
    public Employee? HeadEmployee { get; set; }

    /// <summary>Employees assigned to this department.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
