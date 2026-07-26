using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Employees.GetEmployeesPaged;

/// <summary>
/// Handles <see cref="GetEmployeesPagedQuery"/>: server-side filtering, sorting,
/// and pagination over the employee directory (soft-deleted rows excluded by the
/// global query filter).
/// </summary>
public sealed class GetEmployeesPagedHandler
    : IRequestHandler<GetEmployeesPagedQuery, Result<PagedResult<EmployeeListItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeesPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EmployeeListItem>>> Handle(
        GetEmployeesPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term));
        }

        var descending = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        query = request.SortBy.ToLowerInvariant() switch
        {
            "code" => descending
                ? query.OrderByDescending(e => e.EmployeeCode)
                : query.OrderBy(e => e.EmployeeCode),
            "joindate" => descending
                ? query.OrderByDescending(e => e.JoinDate)
                : query.OrderBy(e => e.JoinDate),
            _ => descending
                ? query.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                : query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeListItem(
                e.Id,
                e.EmployeeCode,
                e.FirstName + " " + e.LastName,
                e.Email,
                e.Department != null ? e.Department.Name : null,
                e.Position != null ? e.Position.Title : null,
                e.Status,
                e.JoinDate))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<EmployeeListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}
