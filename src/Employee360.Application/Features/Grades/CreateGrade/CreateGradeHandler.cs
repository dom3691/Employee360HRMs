using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.CreateGrade;

/// <summary>Handles <see cref="CreateGradeCommand"/> with unique name/level guards.</summary>
public sealed class CreateGradeHandler : IRequestHandler<CreateGradeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateGradeHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateGradeCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _context.Grades.AnyAsync(g => g.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure<Guid>($"A grade named '{name}' already exists.");
        }

        if (await _context.Grades.AnyAsync(g => g.Level == request.Level, cancellationToken))
        {
            return Result.Failure<Guid>($"A grade with level {request.Level} already exists.");
        }

        var grade = new Grade
        {
            Name = name,
            Level = request.Level,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(grade.Id);
    }
}
