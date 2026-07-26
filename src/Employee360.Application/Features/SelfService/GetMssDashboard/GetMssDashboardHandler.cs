using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.SelfService.GetMssDashboard;

/// <summary>Handles <see cref="GetMssDashboardQuery"/> (FR-MSS-001), manager-scoped.</summary>
public sealed class GetMssDashboardHandler
    : IRequestHandler<GetMssDashboardQuery, Result<MssDashboardResponse>>
{
    private const int BirthdayWindowDays = 30;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IManagerScopeService _managerScopeService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMssDashboardHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IManagerScopeService managerScopeService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _managerScopeService = managerScopeService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<MssDashboardResponse>> Handle(
        GetMssDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var managerId = _currentUserService.EmployeeId;

        if (managerId is null)
        {
            return Result.Failure<MssDashboardResponse>(
                "No employee record is linked to your account.");
        }

        var today = _dateTimeProvider.TodayWat;

        var allReports = (await _managerScopeService.GetManagedEmployeeIdsAsync(
            managerId.Value, includeIndirect: true, cancellationToken)).ToList();

        var directReportCount = await _context.Employees
            .CountAsync(e => e.ManagerId == managerId.Value, cancellationToken);

        // Pending approvals: assigned to this manager or anywhere in the subtree.
        var pendingQuery = _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Escalated) &&
                (r.ApproverId == managerId.Value || allReports.Contains(r.EmployeeId)));

        var pendingCount = await pendingQuery.CountAsync(cancellationToken);

        var pendingApprovals = await pendingQuery
            .OrderBy(r => r.CreatedAt)
            .Take(5)
            .Select(r => new LeaveRequestItem(
                r.Id, r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name, r.StartDate, r.EndDate, r.Days, r.Reason, r.Status,
                r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        var onLeaveToday = await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                r.Status == LeaveRequestStatus.Approved &&
                allReports.Contains(r.EmployeeId) &&
                r.StartDate <= today &&
                today <= r.EndDate)
            .Select(r => new OnLeaveTodayItem(
                r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name,
                r.StartDate,
                r.EndDate))
            .ToListAsync(cancellationToken);

        var teamMembers = await _context.Employees
            .AsNoTracking()
            .Where(e => allReports.Contains(e.Id) && e.DateOfBirth != null)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.DateOfBirth })
            .ToListAsync(cancellationToken);

        var upcomingBirthdays = teamMembers
            .Select(e => new
            {
                e.Id,
                Name = $"{e.FirstName} {e.LastName}",
                NextBirthday = NextOccurrence(e.DateOfBirth!.Value, today),
            })
            .Where(e => e.NextBirthday.DayNumber - today.DayNumber <= BirthdayWindowDays)
            .OrderBy(e => e.NextBirthday)
            .Select(e => new UpcomingBirthdayItem(e.Id, e.Name, e.NextBirthday))
            .ToList();

        return Result.Success(new MssDashboardResponse(
            pendingCount,
            pendingApprovals,
            onLeaveToday,
            directReportCount,
            allReports.Count,
            upcomingBirthdays));
    }

    /// <summary>Next anniversary of <paramref name="dateOfBirth"/> on or after today (29 Feb → 1 Mar off leap years).</summary>
    private static DateOnly NextOccurrence(DateOnly dateOfBirth, DateOnly today)
    {
        var month = dateOfBirth.Month;
        var day = dateOfBirth.Day;

        DateOnly Build(int year)
        {
            if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            {
                return new DateOnly(year, 3, 1);
            }

            return new DateOnly(year, month, day);
        }

        var thisYear = Build(today.Year);
        return thisYear >= today ? thisYear : Build(today.Year + 1);
    }
}
