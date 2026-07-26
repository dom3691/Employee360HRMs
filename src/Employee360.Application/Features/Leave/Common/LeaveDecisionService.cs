using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.Common;

/// <summary>
/// Shared decision logic for approve/reject/return (FR-LV-008): approver-scope
/// authorization, balance movement, approval-step recording, and employee
/// notification. Used by the three decision handlers.
/// </summary>
public sealed class LeaveDecisionService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IManagerScopeService _managerScopeService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILeaveNotifier _notifier;

    public LeaveDecisionService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IManagerScopeService managerScopeService,
        IDateTimeProvider dateTimeProvider,
        ILeaveNotifier notifier)
    {
        _context = context;
        _currentUserService = currentUserService;
        _managerScopeService = managerScopeService;
        _dateTimeProvider = dateTimeProvider;
        _notifier = notifier;
    }

    /// <summary>Applies a decision to a pending/escalated request.</summary>
    /// <param name="requestId">The leave request id.</param>
    /// <param name="action">Approve, Reject, or ReturnForRevision.</param>
    /// <param name="comments">Approver comments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<Result> DecideAsync(
        Guid requestId,
        ApprovalAction action,
        string? comments,
        CancellationToken cancellationToken)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure("Leave request not found.");
        }

        if (request.Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Escalated))
        {
            return Result.Failure($"This request has already been {request.Status}.");
        }

        var authorization = await AuthorizeApproverAsync(request, cancellationToken);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        // Release or consume the pending reservation for paid types.
        if (request.LeaveType.IsPaid)
        {
            var balance = await _context.LeaveBalances.FirstOrDefaultAsync(
                b => b.EmployeeId == request.EmployeeId &&
                     b.LeaveTypeId == request.LeaveTypeId &&
                     b.Year == request.StartDate.Year,
                cancellationToken);

            if (balance is not null)
            {
                balance.Pending = Math.Max(0, balance.Pending - request.Days);

                if (action == ApprovalAction.Approve)
                {
                    balance.Used += request.Days;
                }
            }
        }

        request.Status = action switch
        {
            ApprovalAction.Approve => LeaveRequestStatus.Approved,
            ApprovalAction.Reject => LeaveRequestStatus.Rejected,
            _ => LeaveRequestStatus.ReturnedForRevision,
        };

        _context.LeaveApprovals.Add(new LeaveApproval
        {
            LeaveRequestId = request.Id,
            ApproverUserId = _currentUserService.UserId ?? Guid.Empty,
            Action = action,
            Comments = comments,
            ActionAtUtc = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        var decision = action switch
        {
            ApprovalAction.Approve => "Approved",
            ApprovalAction.Reject => "Declined",
            _ => "Returned for Revision",
        };

        await _notifier.NotifyDecisionAsync(request, request.Employee, decision, comments, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> AuthorizeApproverAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        // HR roles may decide any request (Leave.ApproveAll capability).
        if (_currentUserService.IsInRole(RoleNames.HRAdmin) ||
            _currentUserService.IsInRole(RoleNames.HRManager))
        {
            return Result.Success();
        }

        var currentEmployeeId = _currentUserService.EmployeeId;

        if (currentEmployeeId is null)
        {
            return Result.Failure("No employee record is linked to your account.");
        }

        if (request.ApproverId == currentEmployeeId)
        {
            return Result.Success();
        }

        // Indirect managers may act (covers escalations up the chain).
        if (await _managerScopeService.IsManagerOfAsync(
                currentEmployeeId.Value, request.EmployeeId, cancellationToken))
        {
            return Result.Success();
        }

        return Result.Failure("You are not authorized to decide this leave request.");
    }
}
