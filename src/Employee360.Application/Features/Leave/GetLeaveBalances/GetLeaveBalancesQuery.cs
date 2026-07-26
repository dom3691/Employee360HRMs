using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.GetLeaveBalances;

/// <summary>One leave-type balance row (FR-LV-004).</summary>
public sealed record LeaveBalanceItem(
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal Entitled,
    decimal Used,
    decimal Pending,
    decimal CarriedForward,
    decimal Available);

/// <summary>
/// Fetches leave balances for an employee and year. When
/// <paramref name="EmployeeId"/> is null the authenticated user's employee
/// record is used (self-service).
/// </summary>
public sealed record GetLeaveBalancesQuery(Guid? EmployeeId, int Year)
    : IRequest<Result<IReadOnlyList<LeaveBalanceItem>>>;
