using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Leave.Common;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.ApplyLeave;

/// <summary>
/// Handles <see cref="ApplyLeaveCommand"/>: working-days calculation
/// (FR-LV-003), probation and balance validation (FR-LV-006), overlap
/// prevention, pending-day reservation, and manager notification (FR-LV-011).
/// </summary>
public sealed class ApplyLeaveHandler : IRequestHandler<ApplyLeaveCommand, Result<Guid>>
{
    private static readonly LeaveRequestStatus[] BlockingStatuses =
    [
        LeaveRequestStatus.Pending,
        LeaveRequestStatus.Approved,
        LeaveRequestStatus.Escalated,
    ];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkingDaysCalculator _workingDaysCalculator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILeaveNotifier _notifier;

    public ApplyLeaveHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IWorkingDaysCalculator workingDaysCalculator,
        IDateTimeProvider dateTimeProvider,
        ILeaveNotifier notifier)
    {
        _context = context;
        _currentUserService = currentUserService;
        _workingDaysCalculator = workingDaysCalculator;
        _dateTimeProvider = dateTimeProvider;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(ApplyLeaveCommand request, CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<Guid>("No employee record is linked to your account.");
        }

        var employee = await _context.Employees
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<Guid>("Employee record not found.");
        }

        if (employee.Manager is null)
        {
            return Result.Failure<Guid>(
                "You have no line manager assigned to approve leave. Contact HR.");
        }

        var leaveType = await _context.LeaveTypes
            .Include(t => t.Policy)
            .FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId && t.IsActive, cancellationToken);

        if (leaveType is null)
        {
            return Result.Failure<Guid>("Leave type not found or inactive.");
        }

        if (request.StartDate < _dateTimeProvider.TodayWat)
        {
            return Result.Failure<Guid>("Leave cannot start in the past.");
        }

        // Probation exclusion (FR-LV-002).
        if (leaveType.Policy is { ProbationMonths: > 0 } policy && employee.JoinDate.HasValue)
        {
            var probationEnds = employee.JoinDate.Value.AddMonths(policy.ProbationMonths);
            if (request.StartDate < probationEnds)
            {
                return Result.Failure<Guid>(
                    $"{leaveType.Name} is not available during probation (until {probationEnds:dd/MM/yyyy}).");
            }
        }

        var days = await _workingDaysCalculator.CalculateAsync(
            request.StartDate, request.EndDate, cancellationToken);

        if (days <= 0)
        {
            return Result.Failure<Guid>(
                "The selected period contains no working days (weekends/public holidays only).");
        }

        var overlaps = await _context.LeaveRequests.AnyAsync(
            r => r.EmployeeId == employee.Id &&
                 BlockingStatuses.Contains(r.Status) &&
                 r.StartDate <= request.EndDate &&
                 request.StartDate <= r.EndDate,
            cancellationToken);

        if (overlaps)
        {
            return Result.Failure<Guid>(
                "You already have a pending or approved leave request overlapping these dates.");
        }

        // Balance validation and pending reservation for paid types (FR-LV-006).
        if (leaveType.IsPaid)
        {
            var year = request.StartDate.Year;

            var balance = await _context.LeaveBalances.FirstOrDefaultAsync(
                b => b.EmployeeId == employee.Id &&
                     b.LeaveTypeId == leaveType.Id &&
                     b.Year == year,
                cancellationToken);

            if (balance is null)
            {
                if (leaveType.Policy is null)
                {
                    return Result.Failure<Guid>(
                        $"No leave policy is configured for {leaveType.Name}. Contact HR.");
                }

                balance = new LeaveBalance
                {
                    EmployeeId = employee.Id,
                    LeaveTypeId = leaveType.Id,
                    Year = year,
                    Entitled = leaveType.Policy.AnnualEntitlement,
                };
                _context.LeaveBalances.Add(balance);
            }

            if (balance.Available < days)
            {
                return Result.Failure<Guid>(
                    $"Insufficient balance: {balance.Available} day(s) available, {days} requested.");
            }

            balance.Pending += days;
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = days,
            Reason = request.Reason.Trim(),
            Status = LeaveRequestStatus.Pending,
            ApproverId = employee.ManagerId,
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        await _notifier.NotifySubmittedAsync(leaveRequest, employee, employee.Manager, cancellationToken);

        return Result.Success(leaveRequest.Id);
    }
}
