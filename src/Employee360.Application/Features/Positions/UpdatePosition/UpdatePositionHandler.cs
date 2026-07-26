using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Positions.UpdatePosition;

/// <summary>Handles <see cref="UpdatePositionCommand"/>.</summary>
public sealed class UpdatePositionHandler : IRequestHandler<UpdatePositionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdatePositionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken);

        if (position is null)
        {
            return Result.Failure("Position not found.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeTaken = await _context.Positions
            .AnyAsync(p => p.Code == code && p.Id != position.Id, cancellationToken);

        if (codeTaken)
        {
            return Result.Failure($"A position with code '{code}' already exists.");
        }

        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure("Department not found.");
        }

        if (request.GradeId.HasValue &&
            !await _context.Grades.AnyAsync(g => g.Id == request.GradeId.Value, cancellationToken))
        {
            return Result.Failure("Grade not found.");
        }

        position.Title = request.Title.Trim();
        position.Code = code;
        position.DepartmentId = request.DepartmentId;
        position.GradeId = request.GradeId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
