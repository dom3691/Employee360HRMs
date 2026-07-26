using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.ApproveLeave;

/// <summary>Approves a pending leave request (FR-LV-008).</summary>
public sealed record ApproveLeaveCommand(Guid RequestId, string? Comments) : IRequest<Result>;
