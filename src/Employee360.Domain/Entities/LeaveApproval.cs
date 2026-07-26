using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>A single approval-step decision on a leave request (PRD FR-LV-008).</summary>
public class LeaveApproval : BaseEntity
{
    /// <summary>The leave request acted on.</summary>
    public Guid LeaveRequestId { get; set; }

    /// <summary>Navigation to the leave request.</summary>
    public LeaveRequest LeaveRequest { get; set; } = null!;

    /// <summary>User id of the acting approver.</summary>
    public Guid ApproverUserId { get; set; }

    /// <summary>The decision taken.</summary>
    public ApprovalAction Action { get; set; }

    /// <summary>Approver comments (mandatory on reject/return).</summary>
    public string? Comments { get; set; }

    /// <summary>UTC instant of the decision.</summary>
    public DateTime ActionAtUtc { get; set; }
}
