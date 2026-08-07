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

    public GetGradesPagedHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<GradeListItem>>> Handle(
        GetGradesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Grades.AsNoTracking().Where(g => !g.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(g =>
                g.Name.Contains(term) ||
                g.Code.Contains(term) ||
                g.LevelRank.Contains(term) ||
                (g.Description != null && g.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var grades = await query
            .OrderBy(g => g.Level)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = grades.Select(GradeMapping.Map).ToList();

        return Result.Success(new PagedResult<GradeListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}

/// <summary>Handles <see cref="GetGradeByIdQuery"/>.</summary>
public sealed class GetGradeByIdHandler
    : IRequestHandler<GetGradeByIdQuery, Result<GradeListItem>>
{
    private readonly IApplicationDbContext _context;

    public GetGradeByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<GradeListItem>> Handle(
        GetGradeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var grade = await _context.Grades
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GradeId && !g.IsDeleted, cancellationToken);

        return grade is null
            ? Result.Failure<GradeListItem>("Grade not found.")
            : Result.Success(GradeMapping.Map(grade));
    }
}
