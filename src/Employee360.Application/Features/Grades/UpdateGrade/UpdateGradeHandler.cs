using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.UpdateGrade;

/// <summary>Handles <see cref="UpdateGradeCommand"/>.</summary>
public sealed class UpdateGradeHandler : IRequestHandler<UpdateGradeCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateGradeHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateGradeCommand request, CancellationToken cancellationToken)
    {
        var grade = await _context.Grades
            .FirstOrDefaultAsync(g => g.Id == request.GradeId, cancellationToken);

        if (grade is null)
        {
            return Result.Failure("Grade not found.");
        }

        var name = request.Name.Trim();

        if (await _context.Grades.AnyAsync(
                g => g.Name.ToLower() == name.ToLower() && g.Id != grade.Id, cancellationToken))
        {
            return Result.Failure($"A grade named '{name}' already exists.");
        }

        if (await _context.Grades.AnyAsync(
                g => g.Level == request.Level && g.Id != grade.Id, cancellationToken))
        {
            return Result.Failure($"A grade with level {request.Level} already exists.");
        }

        grade.Name = name;
        grade.Level = request.Level;
        grade.MinSalary = request.MinSalary;
        grade.MaxSalary = request.MaxSalary;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
