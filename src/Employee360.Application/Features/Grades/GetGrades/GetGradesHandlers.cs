using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Grades.GetGrades;

/// <summary>Handles <see cref="GetGradesPagedQuery"/>.</summary>
public sealed class GetGradesPagedHandler
    : IRequestHandler<GetGradesPagedQuery, Result<PagedResult<GradeListItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetGradesPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<GradeListItem>>> Handle(
        GetGradesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Grades.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(g => g.Level)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GradeListItem(
                g.Id, g.Name, g.Level, g.MinSalary, g.MaxSalary, g.Positions.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GradeListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}

/// <summary>Handles <see cref="GetGradeByIdQuery"/>.</summary>
public sealed class GetGradeByIdHandler
    : IRequestHandler<GetGradeByIdQuery, Result<GradeListItem>>
{
    private readonly IApplicationDbContext _context;

    public GetGradeByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<GradeListItem>> Handle(
        GetGradeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var grade = await _context.Grades
            .AsNoTracking()
            .Where(g => g.Id == request.GradeId)
            .Select(g => new GradeListItem(
                g.Id, g.Name, g.Level, g.MinSalary, g.MaxSalary, g.Positions.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return grade is null
            ? Result.Failure<GradeListItem>("Grade not found.")
            : Result.Success(grade);
    }
}
