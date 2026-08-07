using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Grades.GetGrades;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Handles <see cref="UpdateGradeCommand"/>.</summary>
public sealed class UpdateGradeHandler : IRequestHandler<UpdateGradeCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateGradeHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateGradeCommand request, CancellationToken cancellationToken)
    {
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == request.GradeId && !g.IsDeleted, cancellationToken);

        if (grade is null)
        {
            return Result.Failure("Grade not found.");
        }

        var title = request.Title.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        var level = GradeMapping.ParseLevelRank(request.LevelRank);

        if (level <= 0)
        {
            return Result.Failure("Level rank must include a numeric level.");
        }

        if (await _context.Grades.AnyAsync(
                g => !g.IsDeleted && g.Name.ToLower() == title.ToLower() && g.Id != grade.Id, cancellationToken))
        {
            return Result.Failure($"A grade named '{title}' already exists.");
        }

        if (await _context.Grades.AnyAsync(
                g => !g.IsDeleted && g.Code.ToLower() == code.ToLower() && g.Id != grade.Id, cancellationToken))
        {
            return Result.Failure($"A grade with code '{code}' already exists.");
        }

        if (await _context.Grades.AnyAsync(
                g => !g.IsDeleted && g.Level == level && g.Id != grade.Id, cancellationToken))
        {
            return Result.Failure($"A grade with level {level} already exists.");
        }

        grade.Name = title;
        grade.Code = code;
        grade.LevelRank = request.LevelRank.Trim();
        grade.Description = request.Description?.Trim();
        grade.Level = level;
        grade.MinSalary = request.SalaryMin;
        grade.MaxSalary = request.SalaryMax;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
