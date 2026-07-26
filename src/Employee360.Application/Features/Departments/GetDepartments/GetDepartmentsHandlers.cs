using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Departments.GetDepartments;

/// <summary>Handles <see cref="GetDepartmentsPagedQuery"/>.</summary>
public sealed class GetDepartmentsPagedHandler
    : IRequestHandler<GetDepartmentsPagedQuery, Result<PagedResult<DepartmentListItem>>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentsPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<DepartmentListItem>>> Handle(
        GetDepartmentsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(term) || d.Code.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DepartmentListItem(
                d.Id,
                d.Name,
                d.Code,
                d.ParentDepartmentId,
                d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                d.HeadEmployeeId,
                d.HeadEmployee != null ? d.HeadEmployee.FirstName + " " + d.HeadEmployee.LastName : null,
                d.Employees.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<DepartmentListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}

/// <summary>Handles <see cref="GetDepartmentByIdQuery"/>.</summary>
public sealed class GetDepartmentByIdHandler
    : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentListItem>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentListItem>> Handle(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == request.DepartmentId)
            .Select(d => new DepartmentListItem(
                d.Id,
                d.Name,
                d.Code,
                d.ParentDepartmentId,
                d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                d.HeadEmployeeId,
                d.HeadEmployee != null ? d.HeadEmployee.FirstName + " " + d.HeadEmployee.LastName : null,
                d.Employees.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return department is null
            ? Result.Failure<DepartmentListItem>("Department not found.")
            : Result.Success(department);
    }
}

/// <summary>Handles <see cref="GetDepartmentTreeQuery"/>.</summary>
public sealed class GetDepartmentTreeHandler
    : IRequestHandler<GetDepartmentTreeQuery, Result<IReadOnlyList<DepartmentTreeNode>>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentTreeHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DepartmentTreeNode>>> Handle(
        GetDepartmentTreeQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Code,
                d.ParentDepartmentId,
                HeadName = d.HeadEmployee != null
                    ? d.HeadEmployee.FirstName + " " + d.HeadEmployee.LastName
                    : null,
            })
            .ToListAsync(cancellationToken);

        var byId = departments.ToDictionary(d => d.Id);
        var childrenByParent = departments
            .Where(d => d.ParentDepartmentId.HasValue)
            .GroupBy(d => d.ParentDepartmentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        DepartmentTreeNode BuildNode(Guid id)
        {
            var department = byId[id];

            var children = childrenByParent.TryGetValue(id, out var kids)
                ? kids.OrderBy(k => k.Name).Select(k => BuildNode(k.Id)).ToList()
                : [];

            return new DepartmentTreeNode(
                department.Id, department.Name, department.Code, department.HeadName, children);
        }

        var roots = departments
            .Where(d => d.ParentDepartmentId is null || !byId.ContainsKey(d.ParentDepartmentId.Value))
            .OrderBy(d => d.Name)
            .Select(d => BuildNode(d.Id))
            .ToList();

        return Result.Success<IReadOnlyList<DepartmentTreeNode>>(roots);
    }
}
