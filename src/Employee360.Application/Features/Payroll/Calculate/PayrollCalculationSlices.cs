using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Payroll.Calculate;

/// <summary>Preview payroll calculation for an employee (FR-PAY-003..007, FR-PAY-012).</summary>
public sealed record PreviewPayrollCalculationQuery(
    Guid EmployeeId,
    int TaxYear,
    DateOnly PayPeriodEnd,
    decimal PayFactor = 1.0m) : IRequest<Result<PayrollCalculationResult>>;

public sealed class PreviewPayrollCalculationValidator : AbstractValidator<PreviewPayrollCalculationQuery>
{
    public PreviewPayrollCalculationValidator()
    {
        RuleFor(q => q.EmployeeId).NotEmpty();
        RuleFor(q => q.TaxYear).GreaterThan(2000);
        RuleFor(q => q.PayFactor).InclusiveBetween(0, 1);
    }
}

public sealed class PreviewPayrollCalculationHandler
    : IRequestHandler<PreviewPayrollCalculationQuery, Result<PayrollCalculationResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly INigeriaPayrollCalculator _calculator;

    public PreviewPayrollCalculationHandler(
        IApplicationDbContext context,
        INigeriaPayrollCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<Result<PayrollCalculationResult>> Handle(
        PreviewPayrollCalculationQuery request,
        CancellationToken cancellationToken)
    {
        var salary = await _context.EmployeeSalaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.EffectiveDate <= request.PayPeriodEnd &&
                        (s.EndDate == null || s.EndDate >= request.PayPeriodEnd))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (salary is null)
        {
            return Result.Failure<PayrollCalculationResult>("No salary assignment effective for this pay period.");
        }

        var bands = await _context.TaxBands
            .AsNoTracking()
            .Where(b => b.TaxYear == request.TaxYear && b.IsActive)
            .OrderBy(b => b.BandOrder)
            .Select(b => new PayrollTaxBandInput(b.BandOrder, b.UpperBoundAnnual, b.Rate))
            .ToListAsync(cancellationToken);

        if (bands.Count == 0)
        {
            return Result.Failure<PayrollCalculationResult>(
                $"No active tax bands configured for tax year {request.TaxYear}.");
        }

        var rates = await _context.PayrollStatutoryRates
            .AsNoTracking()
            .Where(r => r.TaxYear == request.TaxYear && r.IsActive)
            .Select(r => new PayrollStatutoryRateInput(
                r.Code, r.EmployeeRate, r.EmployerRate,
                r.FixedAnnualAmount, r.VariableRate, r.Basis))
            .ToListAsync(cancellationToken);

        if (rates.Count == 0)
        {
            return Result.Failure<PayrollCalculationResult>(
                $"No statutory rates configured for tax year {request.TaxYear}.");
        }

        var deductions = await _context.PayrollDeductions
            .AsNoTracking()
            .Where(d => d.EmployeeId == request.EmployeeId &&
                        d.IsActive &&
                        d.StartDate <= request.PayPeriodEnd &&
                        (d.EndDate == null || d.EndDate >= request.PayPeriodEnd))
            .ToListAsync(cancellationToken);

        var gross = salary.GrossSalary;
        var customDeductions = deductions
            .Select(d =>
            {
                var amount = d.FixedAmount ?? (d.PercentOfGross.HasValue
                    ? gross * d.PercentOfGross.Value
                    : 0m);

                return new PayrollCustomDeductionInput(d.Name, amount);
            })
            .ToList();

        var result = _calculator.Calculate(new PayrollCalculationRequest
        {
            Basic = salary.Basic,
            Housing = salary.Housing,
            Transport = salary.Transport,
            OtherAllowances = salary.OtherAllowances,
            PayFactor = request.PayFactor,
            TaxBands = bands,
            StatutoryRates = rates,
            CustomDeductions = customDeductions,
        });

        return Result.Success(result);
    }
}

/// <summary>
/// Computes pay factor from attendance for a month (FR-PAY-012).
/// </summary>
public static class PayrollPayFactorCalculator
{
    /// <summary>
    /// Returns payable days / expected working days in the period.
    /// Unpaid leave and absent days reduce the factor.
    /// </summary>
    public static decimal Calculate(
        int expectedWorkingDays,
        int unpaidLeaveDays,
        int absentDays)
    {
        if (expectedWorkingDays <= 0)
        {
            return 0m;
        }

        var payableDays = Math.Max(0, expectedWorkingDays - unpaidLeaveDays - absentDays);
        return Math.Round((decimal)payableDays / expectedWorkingDays, 4, MidpointRounding.AwayFromZero);
    }
}
