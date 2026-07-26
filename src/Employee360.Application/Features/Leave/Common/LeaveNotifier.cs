using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Employee360.Application.Features.Leave.Common;

/// <summary>Default <see cref="ILeaveNotifier"/> over <see cref="IEmailService"/>.</summary>
public sealed class LeaveNotifier : ILeaveNotifier
{
    private readonly IEmailService _emailService;
    private readonly ILogger<LeaveNotifier> _logger;

    public LeaveNotifier(IEmailService emailService, ILogger<LeaveNotifier> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task NotifySubmittedAsync(
        LeaveRequest request, Employee employee, Employee approver,
        CancellationToken cancellationToken = default)
        => SendSafelyAsync(
            approver.Email,
            $"Leave Request from {employee.FullName} — Action Required",
            $"""
            <p>{employee.FullName} has requested <strong>{request.Days}</strong> day(s) of leave
            from <strong>{request.StartDate:dd/MM/yyyy}</strong> to <strong>{request.EndDate:dd/MM/yyyy}</strong>.</p>
            <p>Reason: {request.Reason}</p>
            <p>Log in to Employee360 to approve or reject this request.</p>
            """,
            cancellationToken);

    /// <inheritdoc />
    public Task NotifyDecisionAsync(
        LeaveRequest request, Employee employee, string decision, string? comments,
        CancellationToken cancellationToken = default)
        => SendSafelyAsync(
            employee.Email,
            $"Your Leave Request has been {decision}",
            $"""
            <p>Your leave request for <strong>{request.Days}</strong> day(s)
            ({request.StartDate:dd/MM/yyyy} – {request.EndDate:dd/MM/yyyy}) has been
            <strong>{decision}</strong>.</p>
            {(string.IsNullOrEmpty(comments) ? "" : $"<p>Comments: {comments}</p>")}
            """,
            cancellationToken);

    /// <inheritdoc />
    public Task NotifyEscalatedAsync(
        LeaveRequest request, Employee employee, Employee newApprover,
        CancellationToken cancellationToken = default)
        => SendSafelyAsync(
            newApprover.Email,
            $"Escalated: Pending Leave Approval for {employee.FullName}",
            $"""
            <p>A leave request from {employee.FullName}
            ({request.StartDate:dd/MM/yyyy} – {request.EndDate:dd/MM/yyyy}, {request.Days} day(s))
            has exceeded the approval SLA and is now assigned to you.</p>
            """,
            cancellationToken);

    private async Task SendSafelyAsync(
        string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(to, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // Notification failures must never break the leave workflow (FR-LV-011).
            _logger.LogError(ex, "Failed to send leave notification to {Recipient}: {Subject}", to, subject);
        }
    }
}
