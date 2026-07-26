using Employee360.Application.Common.Models;
using Employee360.Application.Features.Leave.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Employee360.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring approval escalation (FR-LV-009): requests pending beyond the SLA
/// (default 48h, "Leave:EscalationSlaHours") are marked Escalated, reassigned to
/// the approver's own manager when one exists, audited, and notified.
/// </summary>
public class LeaveEscalationJob
{
    private readonly Employee360DbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILeaveNotifier _notifier;
    private readonly LeaveSettings _settings;
    private readonly ILogger<LeaveEscalationJob> _logger;

    public LeaveEscalationJob(
        Employee360DbContext context,
        IDateTimeProvider dateTimeProvider,
        ILeaveNotifier notifier,
        IOptions<LeaveSettings> settings,
        ILogger<LeaveEscalationJob> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _notifier = notifier;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Runs one escalation pass.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var cutoff = utcNow.AddHours(-_settings.EscalationSlaHours);

        var overdue = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Approver)
                .ThenInclude(a => a!.Manager)
            .Where(r => r.Status == LeaveRequestStatus.Pending && r.CreatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var request in overdue)
        {
            request.Status = LeaveRequestStatus.Escalated;
            request.EscalatedAtUtc = utcNow;

            var previousApproverId = request.ApproverId;
            var nextApprover = request.Approver?.Manager;

            if (nextApprover is not null)
            {
                request.ApproverId = nextApprover.Id;
            }
            // No higher manager: request stays with the current approver but is
            // flagged Escalated, which surfaces it to HR (Leave.ApproveAll).

            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = nameof(LeaveRequest),
                EntityId = request.Id.ToString(),
                Action = "Escalated",
                OldValues = $$"""{"approverId":"{{previousApproverId}}"}""",
                NewValues = $$"""{"approverId":"{{request.ApproverId}}","slaHours":{{_settings.EscalationSlaHours}}}""",
                UserId = null,
                Timestamp = utcNow,
            });

            if (nextApprover is not null)
            {
                await _notifier.NotifyEscalatedAsync(request, request.Employee, nextApprover, cancellationToken);
            }
        }

        if (overdue.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Leave escalation pass complete: {Count} request(s) escalated (SLA {SlaHours}h)",
            overdue.Count, _settings.EscalationSlaHours);
    }
}
