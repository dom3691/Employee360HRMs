using Employee360.Application.Features.Leave.GetLeaveBalances;
using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.SelfService.GetEssDashboard;

/// <summary>A notification summary row on the dashboard.</summary>
public sealed record NotificationSummary(
    Guid Id,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAtUtc);

/// <summary>
/// Employee Self-Service dashboard payload (FR-ESS-001/002): leave balances,
/// pending requests, recent notifications, and quick actions.
/// </summary>
public sealed record EssDashboardResponse(
    string EmployeeName,
    IReadOnlyList<LeaveBalanceItem> LeaveBalances,
    int PendingRequestCount,
    IReadOnlyList<LeaveRequestItem> PendingRequests,
    int UnreadNotificationCount,
    IReadOnlyList<NotificationSummary> RecentNotifications,
    IReadOnlyList<string> QuickActions);

/// <summary>Builds the ESS dashboard for the authenticated employee.</summary>
public sealed record GetEssDashboardQuery : IRequest<Result<EssDashboardResponse>>;
