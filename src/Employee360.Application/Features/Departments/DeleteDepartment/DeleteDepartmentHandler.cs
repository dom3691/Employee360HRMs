using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Departments.DeleteDepartment;

/// <summary>
/// Handles <see cref="DeleteDepartmentCommand"/>. Removal is converted to a soft
/// delete by the audit interceptor (FR-EMP-005 pattern).
/// </summary>
public sealed class DeleteDepartmentHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteDepartmentHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return Result.Failure("Department not found.");
        }

        var hasChildren = await _context.Departments
            .AnyAsync(d => d.ParentDepartmentId == department.Id, cancellationToken);

        if (hasChildren)
        {
            return Result.Failure(
                "Cannot delete a department that has sub-departments. Reassign or delete them first.");
        }

        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.DepartmentId == department.Id, cancellationToken);

        if (hasEmployees)
        {
            return Result.Failure(
                "Cannot delete a department with assigned employees. Reassign them first.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
