using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.ReturnLeaveForRevision;

/// <summary>Returns a pending leave request to the employee for changes (FR-LV-008).</summary>
public sealed record ReturnLeaveForRevisionCommand(Guid RequestId, string Comments) : IRequest<Result>;
