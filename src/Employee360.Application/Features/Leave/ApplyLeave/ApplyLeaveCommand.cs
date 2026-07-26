using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.ApplyLeave;

/// <summary>
/// Employee self-service leave application (FR-LV-005/006/007). The requesting
/// employee is resolved from the authenticated user; the request routes to their
/// line manager.
/// </summary>
public sealed record ApplyLeaveCommand(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason) : IRequest<Result<Guid>>;
