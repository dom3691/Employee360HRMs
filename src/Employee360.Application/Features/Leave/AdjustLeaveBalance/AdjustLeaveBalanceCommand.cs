using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.AdjustLeaveBalance;

/// <summary>
/// HR manual balance adjustment (FR-LV-012): applies a positive or negative
/// delta to the entitled days, with a mandatory audited reason.
/// </summary>
public sealed record AdjustLeaveBalanceCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal AdjustmentDays,
    string Reason) : IRequest<Result>;
