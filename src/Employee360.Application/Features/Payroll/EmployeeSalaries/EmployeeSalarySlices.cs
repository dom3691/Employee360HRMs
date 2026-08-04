using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.EmployeeSalaries;

public sealed record EmployeeSalaryDto(
    Guid Id,
    Guid EmployeeId,
    Guid? SalaryStructureId,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances,
    decimal GrossSalary,
    DateOnly EffectiveDate,
    DateOnly? EndDate);

public sealed record AssignEmployeeSalaryCommand(
    Guid EmployeeId,
    Guid? SalaryStructureId,
    decimal Basic,
    decimal Housing,
    decimal Transport,
    decimal OtherAllowances,
    DateOnly EffectiveDate) : IRequest<Result<Guid>>;

public sealed class AssignEmployeeSalaryValidator : AbstractValidator<AssignEmployeeSalaryCommand>
{
    public AssignEmployeeSalaryValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.EffectiveDate).NotEmpty();
        RuleFor(c => c.Basic).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Housing).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Transport).GreaterThanOrEqualTo(0);
        RuleFor(c => c.OtherAllowances).GreaterThanOrEqualTo(0);
    }
}

public sealed class AssignEmployeeSalaryHandler : IRequestHandler<AssignEmployeeSalaryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AssignEmployeeSalaryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(AssignEmployeeSalaryCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<Guid>("Employee not found.");
        }

        decimal basic;
        decimal housing;
        decimal transport;
        decimal other;
        Guid? structureId = request.SalaryStructureId;

        if (request.SalaryStructureId.HasValue)
        {
            var structure = await _context.SalaryStructures
                .FirstOrDefaultAsync(s => s.Id == request.SalaryStructureId.Value, cancellationToken);

            if (structure is null)
            {
                return Result.Failure<Guid>("Salary structure not found.");
            }

            basic = structure.Basic;
            housing = structure.Housing;
            transport = structure.Transport;
            other = structure.OtherAllowances;
        }
        else
        {
            basic = request.Basic;
            housing = request.Housing;
            transport = request.Transport;
            other = request.OtherAllowances;
            structureId = null;
        }

        if (basic + housing + transport + other <= 0)
        {
            return Result.Failure<Guid>("At least one salary component must be greater than zero.");
        }

        var current = await _context.EmployeeSalaries
            .Where(s => s.EmployeeId == request.EmployeeId && s.EndDate == null)
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is not null && current.EffectiveDate >= request.EffectiveDate)
        {
            return Result.Failure<Guid>("Effective date must be after the current salary assignment.");
        }

        if (current is not null)
        {
            current.EndDate = request.EffectiveDate.AddDays(-1);
        }

        var assignment = new EmployeeSalary
        {
            EmployeeId = request.EmployeeId,
            SalaryStructureId = structureId,
            Basic = basic,
            Housing = housing,
            Transport = transport,
            OtherAllowances = other,
            EffectiveDate = request.EffectiveDate,
        };

        _context.EmployeeSalaries.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(assignment.Id);
    }
}

public sealed record GetEmployeeSalaryHistoryQuery(Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<EmployeeSalaryDto>>>;

public sealed class GetEmployeeSalaryHistoryHandler
    : IRequestHandler<GetEmployeeSalaryHistoryQuery, Result<IReadOnlyList<EmployeeSalaryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeeSalaryHistoryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<EmployeeSalaryDto>>> Handle(
        GetEmployeeSalaryHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<EmployeeSalaryDto>>("Employee not found.");
        }

        var items = await _context.EmployeeSalaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == request.EmployeeId)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => new EmployeeSalaryDto(
                s.Id, s.EmployeeId, s.SalaryStructureId,
                s.Basic, s.Housing, s.Transport, s.OtherAllowances, s.GrossSalary,
                s.EffectiveDate, s.EndDate))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmployeeSalaryDto>>(items);
    }
}

public sealed record UpdateEmployeePensionDetailsCommand(
    Guid EmployeeId,
    string? PensionPin,
    string? PfaName) : IRequest<Result>;

public sealed class UpdateEmployeePensionDetailsValidator : AbstractValidator<UpdateEmployeePensionDetailsCommand>
{
    public UpdateEmployeePensionDetailsValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.PensionPin).MaximumLength(32);
        RuleFor(c => c.PfaName).MaximumLength(128);
    }
}

public sealed class UpdateEmployeePensionDetailsHandler : IRequestHandler<UpdateEmployeePensionDetailsCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmployeePensionDetailsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateEmployeePensionDetailsCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure("Employee not found.");
        }

        employee.PensionPin = request.PensionPin?.Trim();
        employee.PfaName = request.PfaName?.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
