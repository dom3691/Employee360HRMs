using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>A leave application and its workflow state (PRD FR-LV-005..009).</summary>
public class LeaveRequest : AuditableEntity
{
    /// <summary>The requesting employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the requesting employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>The leave type.</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>Navigation to the leave type.</summary>
    public LeaveType LeaveType { get; set; } = null!;

    /// <summary>First day of leave.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Last day of leave (inclusive).</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Working days requested (weekends and public holidays excluded).</summary>
    public decimal Days { get; set; }

    /// <summary>Reason supplied by the employee.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Workflow status.</summary>
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    /// <summary>Employee id of the current approver (line manager; moves on escalation).</summary>
    public Guid? ApproverId { get; set; }

    /// <summary>Navigation to the current approver.</summary>
    public Employee? Approver { get; set; }

    /// <summary>UTC instant the request was escalated, or null.</summary>
    public DateTime? EscalatedAtUtc { get; set; }

    /// <summary>Approval step history.</summary>
    public ICollection<LeaveApproval> Approvals { get; set; } = new List<LeaveApproval>();
}
