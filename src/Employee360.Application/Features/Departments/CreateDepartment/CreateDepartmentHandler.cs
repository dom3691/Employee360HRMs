using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Departments.CreateDepartment;

/// <summary>Handles <see cref="CreateDepartmentCommand"/> with unique-code and reference guards.</summary>
public sealed class CreateDepartmentHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateDepartmentHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeTaken = await _context.Departments
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Code == code && !d.IsDeleted, cancellationToken);

        if (codeTaken)
        {
            return Result.Failure<Guid>($"A department with code '{code}' already exists.");
        }

        if (request.ParentDepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == request.ParentDepartmentId.Value, cancellationToken))
        {
            return Result.Failure<Guid>("Parent department not found.");
        }

        if (request.HeadEmployeeId.HasValue &&
            !await _context.Employees.AnyAsync(e => e.Id == request.HeadEmployeeId.Value, cancellationToken))
        {
            return Result.Failure<Guid>("Head employee not found.");
        }

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            ParentDepartmentId = request.ParentDepartmentId,
            HeadEmployeeId = request.HeadEmployeeId,
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(department.Id);
    }
}
