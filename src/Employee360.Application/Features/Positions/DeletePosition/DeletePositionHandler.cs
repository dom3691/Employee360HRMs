using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Positions.DeletePosition;

/// <summary>Handles <see cref="DeletePositionCommand"/> (soft delete via interceptor).</summary>
public sealed class DeletePositionHandler : IRequestHandler<DeletePositionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeletePositionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeletePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken);

        if (position is null)
        {
            return Result.Failure("Position not found.");
        }

        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.PositionId == position.Id, cancellationToken);

        if (hasEmployees)
        {
            return Result.Failure(
                "Cannot delete a position held by employees. Reassign them first.");
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
