using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.GetLeaveRequests;

/// <summary>Handles <see cref="GetMyLeaveRequestsQuery"/>.</summary>
public sealed class GetMyLeaveRequestsHandler
    : IRequestHandler<GetMyLeaveRequestsQuery, Result<IReadOnlyList<LeaveRequestItem>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyLeaveRequestsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveRequestItem>>> Handle(
        GetMyLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<IReadOnlyList<LeaveRequestItem>>(
                "No employee record is linked to your account.");
        }

        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId.Value);

        if (request.Year.HasValue)
        {
            query = query.Where(r => r.StartDate.Year == request.Year.Value);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LeaveRequestItem(
                r.Id,
                r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name,
                r.StartDate,
                r.EndDate,
                r.Days,
                r.Reason,
                r.Status,
                r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LeaveRequestItem>>(items);
    }
}

/// <summary>Handles <see cref="GetApprovalQueueQuery"/> (manager-scoped).</summary>
public sealed class GetApprovalQueueHandler
    : IRequestHandler<GetApprovalQueueQuery, Result<IReadOnlyList<LeaveRequestItem>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IManagerScopeService _managerScopeService;

    public GetApprovalQueueHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IManagerScopeService managerScopeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _managerScopeService = managerScopeService;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveRequestItem>>> Handle(
        GetApprovalQueueQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<IReadOnlyList<LeaveRequestItem>>(
                "No employee record is linked to your account.");
        }

        var managedIds = await _managerScopeService.GetManagedEmployeeIdsAsync(
            employeeId.Value, includeIndirect: true, cancellationToken);
        var managedSet = managedIds.ToList();

        var items = await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Escalated) &&
                (r.ApproverId == employeeId.Value || managedSet.Contains(r.EmployeeId)))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new LeaveRequestItem(
                r.Id,
                r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name,
                r.StartDate,
                r.EndDate,
                r.Days,
                r.Reason,
                r.Status,
                r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LeaveRequestItem>>(items);
    }
}

/// <summary>Handles <see cref="GetTeamLeaveCalendarQuery"/> (FR-LV-010).</summary>
public sealed class GetTeamLeaveCalendarHandler
    : IRequestHandler<GetTeamLeaveCalendarQuery, Result<IReadOnlyList<TeamCalendarEntry>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IManagerScopeService _managerScopeService;

    public GetTeamLeaveCalendarHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IManagerScopeService managerScopeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _managerScopeService = managerScopeService;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TeamCalendarEntry>>> Handle(
        GetTeamLeaveCalendarQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<IReadOnlyList<TeamCalendarEntry>>(
                "No employee record is linked to your account.");
        }

        var managedIds = (await _managerScopeService.GetManagedEmployeeIdsAsync(
            employeeId.Value, includeIndirect: true, cancellationToken)).ToList();

        // The manager's own leave shows on their team calendar too.
        managedIds.Add(employeeId.Value);

        var entries = await _context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                managedIds.Contains(r.EmployeeId) &&
                (r.Status == LeaveRequestStatus.Approved || r.Status == LeaveRequestStatus.Pending) &&
                r.StartDate <= request.To &&
                request.From <= r.EndDate)
            .OrderBy(r => r.StartDate)
            .Select(r => new TeamCalendarEntry(
                r.EmployeeId,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType.Name,
                r.LeaveType.Color,
                r.StartDate,
                r.EndDate,
                r.Status))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TeamCalendarEntry>>(entries);
    }
}
