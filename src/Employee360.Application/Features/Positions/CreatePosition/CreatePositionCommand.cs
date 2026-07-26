using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Positions.CreatePosition;

/// <summary>Creates a position / job title (FR-EMP-010).</summary>
/// <param name="Title">Position title.</param>
/// <param name="Code">Unique short code, e.g. "SR-ACC".</param>
/// <param name="DepartmentId">Optional owning department.</param>
/// <param name="GradeId">Optional salary grade.</param>
public sealed record CreatePositionCommand(
    string Title,
    string Code,
    Guid? DepartmentId,
    Guid? GradeId) : IRequest<Result<Guid>>;
