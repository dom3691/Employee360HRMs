using Employee360.Domain.Entities;

namespace Employee360.Application.Features.Leave.Common;

/// <summary>
/// Sends leave workflow email notifications (PRD FR-LV-011: submit, approve,
/// reject, escalate). Failures are logged and never break the workflow.
/// </summary>
public interface ILeaveNotifier
{
    /// <summary>Notifies the approver that a request awaits action (EML-001).</summary>
    Task NotifySubmittedAsync(
        LeaveRequest request, Employee employee, Employee approver,
        CancellationToken cancellationToken = default);

    /// <summary>Notifies the employee of an approve/reject/return decision (EML-002/003).</summary>
    Task NotifyDecisionAsync(
        LeaveRequest request, Employee employee, string decision, string? comments,
        CancellationToken cancellationToken = default);

    /// <summary>Notifies the escalation recipient of an overdue approval (EML-004).</summary>
    Task NotifyEscalatedAsync(
        LeaveRequest request, Employee employee, Employee newApprover,
        CancellationToken cancellationToken = default);
}
