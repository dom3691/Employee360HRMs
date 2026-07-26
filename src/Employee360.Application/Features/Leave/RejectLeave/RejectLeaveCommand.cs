using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.RejectLeave;

/// <summary>Rejects a pending leave request with a mandatory comment (FR-LV-008).</summary>
public sealed record RejectLeaveCommand(Guid RequestId, string Comments) : IRequest<Result>;
