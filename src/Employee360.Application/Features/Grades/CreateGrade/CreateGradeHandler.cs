using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Grades.GetGrades;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.CreateGrade;

/// <summary>Handles <see cref="CreateGradeCommand"/> with unique name/level guards.</summary>
public sealed class CreateGradeHandler : IRequestHandler<CreateGradeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateGradeHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateGradeCommand request, CancellationToken cancellationToken)
    {
        var title = request.Title.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        var level = GradeMapping.ParseLevelRank(request.LevelRank);

        if (level <= 0)
        {
            return Result.Failure<Guid>("Level rank must include a numeric level.");
        }

        if (await _context.Grades.AnyAsync(g => !g.IsDeleted && g.Name.ToLower() == title.ToLower(), cancellationToken))
        {
            return Result.Failure<Guid>($"A grade named '{title}' already exists.");
        }

        if (await _context.Grades.AnyAsync(g => !g.IsDeleted && g.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            return Result.Failure<Guid>($"A grade with code '{code}' already exists.");
        }

        if (await _context.Grades.AnyAsync(g => !g.IsDeleted && g.Level == level, cancellationToken))
        {
            return Result.Failure<Guid>($"A grade with level {level} already exists.");
        }

        var grade = new Grade
        {
            Name = title,
            Code = code,
            LevelRank = request.LevelRank.Trim(),
            Description = request.Description?.Trim(),
            Level = level,
            MinSalary = request.SalaryMin,
            MaxSalary = request.SalaryMax,
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(grade.Id);
    }
}
