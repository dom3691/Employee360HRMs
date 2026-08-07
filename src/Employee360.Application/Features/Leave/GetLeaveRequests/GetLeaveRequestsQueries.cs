using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Leave.GetLeaveRequests;

/// <summary>A leave request row.</summary>
public sealed record LeaveRequestItem(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string? DepartmentName,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Days,
    string Reason,
    LeaveRequestStatus Status,
    string? ApproverName,
    DateTime SubmittedAtUtc);

/// <summary>The authenticated employee's own requests, optionally by year.</summary>
public sealed record GetMyLeaveRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    int? Year = null)
    : IRequest<Result<PagedResult<LeaveRequestItem>>>;

/// <summary>Org-wide HR leave requests view (FR-LV HR dashboard).</summary>
public sealed record GetAllLeaveRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    int? Year = null,
    string? Search = null,
    LeaveRequestStatus? Status = null,
    string? LeaveType = null)
    : IRequest<Result<PagedResult<LeaveRequestItem>>>;

/// <summary>
/// Pending/escalated requests awaiting the authenticated manager — direct
/// assignments plus anything in their reporting subtree (FR-AUTH-008 scope).
/// </summary>
public sealed record GetApprovalQueueQuery(
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<LeaveRequestItem>>>;

/// <summary>A calendar entry for team leave (FR-LV-010).</summary>
public sealed record TeamCalendarEntry(
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    string? LeaveTypeColor,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveRequestStatus Status);

/// <summary>Team leave calendar over a date range (manager-scoped).</summary>
public sealed record GetTeamLeaveCalendarQuery(DateOnly From, DateOnly To)
    : IRequest<Result<IReadOnlyList<TeamCalendarEntry>>>;
