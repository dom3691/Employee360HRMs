using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Positions.UpdatePosition;

/// <summary>Updates a position / job title.</summary>
public sealed record UpdatePositionCommand(
    Guid PositionId,
    string Title,
    string Code,
    Guid? DepartmentId,
    Guid? GradeId) : IRequest<Result>;
