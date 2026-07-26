using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Core employee record. Introduced in minimal form for the reporting hierarchy
/// (manager scoping, PRD FR-AUTH-008); the full master-data field set
/// (FR-EMP-002/003) is added by the Employee Management slices in Batch 5.
/// </summary>
public class Employee : AuditableEntity, ISoftDelete
{
    /// <summary>First name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Work email (unique among active employees).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The employee's line manager, or null for the top of the hierarchy.</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Navigation to the line manager.</summary>
    public Employee? Manager { get; set; }

    /// <summary>Direct reports (inverse of <see cref="Manager"/>).</summary>
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    /// <summary>Lifecycle status (PRD FR-EMP-004).</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Draft;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <summary>Full display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
