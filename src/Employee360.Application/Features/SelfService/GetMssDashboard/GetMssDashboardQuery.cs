using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.SelfService.GetMssDashboard;

/// <summary>A team member currently on approved leave.</summary>
public sealed record OnLeaveTodayItem(
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate);

/// <summary>An upcoming team birthday (next 30 days).</summary>
public sealed record UpcomingBirthdayItem(
    Guid EmployeeId,
    string EmployeeName,
    DateOnly NextBirthday);

/// <summary>
/// Manager Self-Service dashboard payload (FR-MSS-001): pending approvals,
/// team on leave today, team headcount, and upcoming birthdays.
/// </summary>
public sealed record MssDashboardResponse(
    int PendingApprovalCount,
    IReadOnlyList<LeaveRequestItem> PendingApprovals,
    IReadOnlyList<OnLeaveTodayItem> TeamOnLeaveToday,
    int DirectReportCount,
    int TotalTeamCount,
    IReadOnlyList<UpcomingBirthdayItem> UpcomingBirthdays);

/// <summary>Builds the MSS dashboard for the authenticated manager.</summary>
public sealed record GetMssDashboardQuery : IRequest<Result<MssDashboardResponse>>;
