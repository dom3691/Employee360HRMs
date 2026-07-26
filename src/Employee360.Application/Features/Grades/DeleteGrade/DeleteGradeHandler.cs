using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.DeleteGrade;

/// <summary>Handles <see cref="DeleteGradeCommand"/> (soft delete via interceptor).</summary>
public sealed class DeleteGradeHandler : IRequestHandler<DeleteGradeCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteGradeHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteGradeCommand request, CancellationToken cancellationToken)
    {
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == request.GradeId, cancellationToken);

        if (grade is null)
        {
            return Result.Failure("Grade not found.");
        }

        var hasPositions = await _context.Positions
            .AnyAsync(p => p.GradeId == grade.Id, cancellationToken);

        if (hasPositions)
        {
            return Result.Failure(
                "Cannot delete a grade referenced by positions. Reassign them first.");
        }

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
