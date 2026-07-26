using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Employee-initiated profile change awaiting HR approval (PRD FR-EMP-011).
/// Requested field changes are stored as a JSON object of field → new value.
/// </summary>
public class ProfileChangeRequest : AuditableEntity
{
    /// <summary>The employee requesting the change.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>JSON object of requested changes, e.g. {"PhoneNumber":"+234..."}.</summary>
    public string ChangesJson { get; set; } = string.Empty;

    /// <summary>Workflow state.</summary>
    public ProfileChangeRequestStatus Status { get; set; } = ProfileChangeRequestStatus.Pending;

    /// <summary>Reviewing HR user id, when reviewed.</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>UTC instant of the review decision.</summary>
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Reviewer comment (mandatory on rejection).</summary>
    public string? ReviewComment { get; set; }
}
