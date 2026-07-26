using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Leave.GetLeaveBalances;
using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.SelfService.GetEssDashboard;

/// <summary>Handles <see cref="GetEssDashboardQuery"/> (FR-ESS-001/002).</summary>
public sealed class GetEssDashboardHandler
    : IRequestHandler<GetEssDashboardQuery, Result<EssDashboardResponse>>
{
    /// <summary>Quick actions surfaced on the dashboard (FR-ESS-002).</summary>
    public static readonly IReadOnlyList<string> QuickActions =
        ["apply-leave", "view-profile", "view-documents"];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEssDashboardHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<EssDashboardResponse>> Handle(
        GetEssDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<EssDashboardResponse>(
                "No employee record is linked to your account.");
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<EssDashboardResponse>("Employee record not found.");
        }

        var year = _dateTimeProvider.TodayWat.Year;

        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId.Value && b.Year == year)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new LeaveBalanceItem(
                b.LeaveTypeId, b.LeaveType.Name, b.Year,
                b.Entitled, b.Used, b.Pending, b.CarriedForward,
                b.Entitled + b.CarriedForward - b.Used - b.Pending))
            .ToListAsync(cancellationToken);

        var pendingQuery = _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId.Value &&
                        (r.Status == LeaveRequestStatus.Pending ||
                         r.Status == LeaveRequestStatus.Escalated));

        var pendingCount = await pendingQuery.CountAsync(cancellationToken);

        var pendingRequests = await pendingQuery
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new LeaveRequestItem(
                r.Id, r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name, r.StartDate, r.EndDate, r.Days, r.Reason, r.Status,
                r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        var userId = _currentUserService.UserId;

        var unreadCount = 0;
        var recentNotifications = new List<NotificationSummary>();

        if (userId is not null)
        {
            unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);

            recentNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationSummary(
                    n.Id, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        return Result.Success(new EssDashboardResponse(
            employee.FullName,
            balances,
            pendingCount,
            pendingRequests,
            unreadCount,
            recentNotifications,
            QuickActions));
    }
}
