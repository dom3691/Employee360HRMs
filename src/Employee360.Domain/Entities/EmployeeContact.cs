using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>Emergency contact / next of kin (PRD FR-EMP-013).</summary>
public class EmployeeContact : AuditableEntity
{
    /// <summary>Owning employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the owning employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Contact type: emergency contact or next of kin.</summary>
    public ContactType Type { get; set; }

    /// <summary>Contact person's full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Relationship to the employee, e.g. "Spouse".</summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>Contact phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Contact address (optional).</summary>
    public string? Address { get; set; }
}
