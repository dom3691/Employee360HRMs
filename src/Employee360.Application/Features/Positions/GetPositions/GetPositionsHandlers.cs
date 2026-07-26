using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Positions.GetPositions;

/// <summary>Handles <see cref="GetPositionsPagedQuery"/>.</summary>
public sealed class GetPositionsPagedHandler
    : IRequestHandler<GetPositionsPagedQuery, Result<PagedResult<PositionListItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetPositionsPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<PositionListItem>>> Handle(
        GetPositionsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Positions.AsNoTracking().AsQueryable();

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(p => p.DepartmentId == request.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) || p.Code.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Title)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PositionListItem(
                p.Id,
                p.Title,
                p.Code,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.GradeId,
                p.Grade != null ? p.Grade.Name : null,
                p.Employees.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<PositionListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}

/// <summary>Handles <see cref="GetPositionByIdQuery"/>.</summary>
public sealed class GetPositionByIdHandler
    : IRequestHandler<GetPositionByIdQuery, Result<PositionListItem>>
{
    private readonly IApplicationDbContext _context;

    public GetPositionByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PositionListItem>> Handle(
        GetPositionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Where(p => p.Id == request.PositionId)
            .Select(p => new PositionListItem(
                p.Id,
                p.Title,
                p.Code,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.GradeId,
                p.Grade != null ? p.Grade.Name : null,
                p.Employees.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return position is null
            ? Result.Failure<PositionListItem>("Position not found.")
            : Result.Success(position);
    }
}
