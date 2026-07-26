using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Departments.UpdateDepartment;

/// <summary>
/// Handles <see cref="UpdateDepartmentCommand"/>. Prevents circular hierarchies:
/// the new parent must not be the department itself or any of its descendants.
/// </summary>
public sealed class UpdateDepartmentHandler : IRequestHandler<UpdateDepartmentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateDepartmentHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return Result.Failure("Department not found.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeTaken = await _context.Departments
            .AnyAsync(d => d.Code == code && d.Id != department.Id, cancellationToken);

        if (codeTaken)
        {
            return Result.Failure($"A department with code '{code}' already exists.");
        }

        if (request.HeadEmployeeId.HasValue &&
            !await _context.Employees.AnyAsync(e => e.Id == request.HeadEmployeeId.Value, cancellationToken))
        {
            return Result.Failure("Head employee not found.");
        }

        if (request.ParentDepartmentId.HasValue &&
            request.ParentDepartmentId != department.ParentDepartmentId)
        {
            if (request.ParentDepartmentId.Value == department.Id)
            {
                return Result.Failure("A department cannot be its own parent.");
            }

            if (!await _context.Departments.AnyAsync(
                    d => d.Id == request.ParentDepartmentId.Value, cancellationToken))
            {
                return Result.Failure("Parent department not found.");
            }

            if (await WouldCreateCycleAsync(
                    department.Id, request.ParentDepartmentId.Value, cancellationToken))
            {
                return Result.Failure(
                    "Invalid hierarchy: the selected parent is a descendant of this department.");
            }
        }

        department.Name = request.Name.Trim();
        department.Code = code;
        department.ParentDepartmentId = request.ParentDepartmentId;
        department.HeadEmployeeId = request.HeadEmployeeId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// True when <paramref name="proposedParentId"/> sits anywhere in the subtree
    /// rooted at <paramref name="departmentId"/> (which would create a cycle).
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(
        Guid departmentId,
        Guid proposedParentId,
        CancellationToken cancellationToken)
    {
        var pairs = await _context.Departments
            .AsNoTracking()
            .Where(d => d.ParentDepartmentId != null)
            .Select(d => new { d.Id, ParentId = d.ParentDepartmentId!.Value })
            .ToListAsync(cancellationToken);

        var childrenByParent = pairs
            .GroupBy(p => p.ParentId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == proposedParentId)
            {
                return true;
            }

            if (!childrenByParent.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children.Where(visited.Add))
            {
                queue.Enqueue(child);
            }
        }

        return false;
    }
}
