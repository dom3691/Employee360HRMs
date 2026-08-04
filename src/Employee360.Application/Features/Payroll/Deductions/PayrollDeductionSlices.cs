using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.Deductions;

public sealed record PayrollDeductionDto(
    Guid Id,
    Guid EmployeeId,
    string Name,
    PayrollDeductionType DeductionType,
    decimal? FixedAmount,
    decimal? PercentOfGross,
    bool IsRecurring,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record GetPayrollDeductionsQuery(Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<PayrollDeductionDto>>>;

public sealed class GetPayrollDeductionsHandler
    : IRequestHandler<GetPayrollDeductionsQuery, Result<IReadOnlyList<PayrollDeductionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollDeductionsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<PayrollDeductionDto>>> Handle(
        GetPayrollDeductionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.PayrollDeductions
            .AsNoTracking()
            .Where(d => d.EmployeeId == request.EmployeeId)
            .OrderBy(d => d.Name)
            .Select(d => Map(d))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PayrollDeductionDto>>(items);
    }

    internal static PayrollDeductionDto Map(PayrollDeduction d) =>
        new(d.Id, d.EmployeeId, d.Name, d.DeductionType, d.FixedAmount, d.PercentOfGross,
            d.IsRecurring, d.StartDate, d.EndDate, d.IsActive);
}

public sealed record CreatePayrollDeductionCommand(
    Guid EmployeeId,
    string Name,
    PayrollDeductionType DeductionType,
    decimal? FixedAmount,
    decimal? PercentOfGross,
    bool IsRecurring,
    DateOnly StartDate,
    DateOnly? EndDate) : IRequest<Result<Guid>>;

public sealed class CreatePayrollDeductionValidator : AbstractValidator<CreatePayrollDeductionCommand>
{
    public CreatePayrollDeductionValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c)
            .Must(c => c.FixedAmount > 0 || c.PercentOfGross > 0)
            .WithMessage("Either fixed amount or percent of gross is required.");
    }
}

public sealed class CreatePayrollDeductionHandler : IRequestHandler<CreatePayrollDeductionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreatePayrollDeductionHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreatePayrollDeductionCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<Guid>("Employee not found.");
        }

        var deduction = new PayrollDeduction
        {
            EmployeeId = request.EmployeeId,
            Name = request.Name.Trim(),
            DeductionType = request.DeductionType,
            FixedAmount = request.FixedAmount,
            PercentOfGross = request.PercentOfGross,
            IsRecurring = request.IsRecurring,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
        };

        _context.PayrollDeductions.Add(deduction);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(deduction.Id);
    }
}

public sealed record UpdatePayrollDeductionCommand(
    Guid Id,
    string Name,
    PayrollDeductionType DeductionType,
    decimal? FixedAmount,
    decimal? PercentOfGross,
    bool IsRecurring,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive) : IRequest<Result>;

public sealed class UpdatePayrollDeductionValidator : AbstractValidator<UpdatePayrollDeductionCommand>
{
    public UpdatePayrollDeductionValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdatePayrollDeductionHandler : IRequestHandler<UpdatePayrollDeductionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdatePayrollDeductionHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdatePayrollDeductionCommand request, CancellationToken cancellationToken)
    {
        var deduction = await _context.PayrollDeductions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deduction is null)
        {
            return Result.Failure("Payroll deduction not found.");
        }

        deduction.Name = request.Name.Trim();
        deduction.DeductionType = request.DeductionType;
        deduction.FixedAmount = request.FixedAmount;
        deduction.PercentOfGross = request.PercentOfGross;
        deduction.IsRecurring = request.IsRecurring;
        deduction.StartDate = request.StartDate;
        deduction.EndDate = request.EndDate;
        deduction.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeletePayrollDeductionCommand(Guid Id) : IRequest<Result>;

public sealed class DeletePayrollDeductionHandler : IRequestHandler<DeletePayrollDeductionCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeletePayrollDeductionHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeletePayrollDeductionCommand request, CancellationToken cancellationToken)
    {
        var deduction = await _context.PayrollDeductions
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deduction is null)
        {
            return Result.Failure("Payroll deduction not found.");
        }

        deduction.IsDeleted = true;
        deduction.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
