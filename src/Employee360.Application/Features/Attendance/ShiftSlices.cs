using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Attendance;

// ---------------------------------------------------------------------------
// Shift DTOs
// ---------------------------------------------------------------------------

/// <summary>Shift list/detail item (FR-ATT-002).</summary>
public sealed record ShiftDto(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GraceMinutes,
    int BreakMinutes,
    int DepartmentCount);

// ---------------------------------------------------------------------------
// CreateShift
// ---------------------------------------------------------------------------

/// <summary>Creates a work shift definition.</summary>
public sealed record CreateShiftCommand(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GraceMinutes,
    int BreakMinutes) : IRequest<Result<Guid>>;

/// <summary>Input validation for <see cref="CreateShiftCommand"/>.</summary>
public sealed class CreateShiftValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(64);
        RuleFor(c => c.GraceMinutes).GreaterThanOrEqualTo(0);
        RuleFor(c => c.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handles <see cref="CreateShiftCommand"/>.</summary>
public sealed class CreateShiftHandler : IRequestHandler<CreateShiftCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateShiftHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _context.Shifts.AnyAsync(s => s.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            return Result.Failure<Guid>($"A shift named '{name}' already exists.");
        }

        var shift = new Shift
        {
            Name = name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GraceMinutes = request.GraceMinutes,
            BreakMinutes = request.BreakMinutes,
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(shift.Id);
    }
}

// ---------------------------------------------------------------------------
// UpdateShift
// ---------------------------------------------------------------------------

/// <summary>Updates a work shift definition.</summary>
public sealed record UpdateShiftCommand(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GraceMinutes,
    int BreakMinutes) : IRequest<Result>;

/// <summary>Input validation for <see cref="UpdateShiftCommand"/>.</summary>
public sealed class UpdateShiftValidator : AbstractValidator<UpdateShiftCommand>
{
    public UpdateShiftValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(64);
        RuleFor(c => c.GraceMinutes).GreaterThanOrEqualTo(0);
        RuleFor(c => c.BreakMinutes).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handles <see cref="UpdateShiftCommand"/>.</summary>
public sealed class UpdateShiftHandler : IRequestHandler<UpdateShiftCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateShiftHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shift is null)
        {
            return Result.Failure("Shift not found.");
        }

        var name = request.Name.Trim();

        if (await _context.Shifts.AnyAsync(
                s => s.Id != request.Id && s.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Result.Failure($"A shift named '{name}' already exists.");
        }

        shift.Name = name;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.GraceMinutes = request.GraceMinutes;
        shift.BreakMinutes = request.BreakMinutes;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// DeleteShift
// ---------------------------------------------------------------------------

/// <summary>Soft-deletes a shift (blocked when departments are assigned).</summary>
public sealed record DeleteShiftCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteShiftCommand"/>.</summary>
public sealed class DeleteShiftHandler : IRequestHandler<DeleteShiftCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteShiftHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shift is null)
        {
            return Result.Failure("Shift not found.");
        }

        if (await _context.Departments.AnyAsync(d => d.ShiftId == request.Id, cancellationToken))
        {
            return Result.Failure("Cannot delete a shift that is assigned to departments.");
        }

        shift.IsDeleted = true;
        shift.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// GetShifts
// ---------------------------------------------------------------------------

/// <summary>Lists shifts (paginated).</summary>
public sealed record GetShiftsPagedQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ShiftDto>>>;

/// <summary>Input validation for <see cref="GetShiftsPagedQuery"/>.</summary>
public sealed class GetShiftsPagedValidator : AbstractValidator<GetShiftsPagedQuery>
{
    public GetShiftsPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Handles <see cref="GetShiftsPagedQuery"/>.</summary>
public sealed class GetShiftsPagedHandler : IRequestHandler<GetShiftsPagedQuery, Result<PagedResult<ShiftDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetShiftsPagedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ShiftDto>>> Handle(
        GetShiftsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Shifts.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShiftDto(
                s.Id, s.Name, s.StartTime, s.EndTime, s.GraceMinutes, s.BreakMinutes, s.Departments.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ShiftDto>(
            items, request.Page, request.PageSize, totalCount));
    }
}

// ---------------------------------------------------------------------------
// AssignShiftToDepartment
// ---------------------------------------------------------------------------

/// <summary>Assigns a shift to a department (FR-ATT-002).</summary>
public sealed record AssignShiftToDepartmentCommand(Guid DepartmentId, Guid? ShiftId) : IRequest<Result>;

/// <summary>Input validation for <see cref="AssignShiftToDepartmentCommand"/>.</summary>
public sealed class AssignShiftToDepartmentValidator : AbstractValidator<AssignShiftToDepartmentCommand>
{
    public AssignShiftToDepartmentValidator()
    {
        RuleFor(c => c.DepartmentId).NotEmpty();
    }
}

/// <summary>Handles <see cref="AssignShiftToDepartmentCommand"/>.</summary>
public sealed class AssignShiftToDepartmentHandler : IRequestHandler<AssignShiftToDepartmentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignShiftToDepartmentHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        AssignShiftToDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return Result.Failure("Department not found.");
        }

        if (request.ShiftId.HasValue &&
            !await _context.Shifts.AnyAsync(s => s.Id == request.ShiftId.Value, cancellationToken))
        {
            return Result.Failure("Shift not found.");
        }

        department.ShiftId = request.ShiftId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
