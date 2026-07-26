using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Positions.DeletePosition;

/// <summary>Soft-deletes a position. Blocked while employees hold it.</summary>
/// <param name="PositionId">The position to delete.</param>
public sealed record DeletePositionCommand(Guid PositionId) : IRequest<Result>;
