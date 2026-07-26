using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Positions.CreatePosition;

/// <summary>Handles <see cref="CreatePositionCommand"/> with unique-code and reference guards.</summary>
public sealed class CreatePositionHandler : IRequestHandler<CreatePositionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreatePositionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeTaken = await _context.Positions
            .AnyAsync(p => p.Code == code, cancellationToken);

        if (codeTaken)
        {
            return Result.Failure<Guid>($"A position with code '{code}' already exists.");
        }

        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            return Result.Failure<Guid>("Department not found.");
        }

        if (request.GradeId.HasValue &&
            !await _context.Grades.AnyAsync(g => g.Id == request.GradeId.Value, cancellationToken))
        {
            return Result.Failure<Guid>("Grade not found.");
        }

        var position = new Position
        {
            Title = request.Title.Trim(),
            Code = code,
            DepartmentId = request.DepartmentId,
            GradeId = request.GradeId,
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(position.Id);
    }
}
