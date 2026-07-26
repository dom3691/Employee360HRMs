using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Leave.CreateLeaveType;

/// <summary>Creates a leave type (FR-LV-001). HR configuration.</summary>
public sealed record CreateLeaveTypeCommand(
    string Name,
    string Code,
    bool IsPaid,
    bool RequiresAttachment,
    string? Color) : IRequest<Result<Guid>>;
