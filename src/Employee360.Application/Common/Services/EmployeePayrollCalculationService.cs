using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Features.Payroll.Calculate;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Loads employee salary, config, attendance/leave pay factor, and runs the Nigeria calculator.
/// </summary>
public interface IEmployeePayrollCalculationService
{
    Task<Result<PayrollCalculationResult>> CalculateForEmployeeAsync(
        Guid employeeId,
        int taxYear,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default);
}

public sealed class EmployeePayrollCalculationService : IEmployeePayrollCalculationService
{
    private readonly IApplicationDbContext _context;
    private readonly INigeriaPayrollCalculator _calculator;
    private readonly PayrollSettings _settings;

    public EmployeePayrollCalculationService(
        IApplicationDbContext context,
        INigeriaPayrollCalculator calculator,
        IOptions<PayrollSettings> settings)
    {
        _context = context;
        _calculator = calculator;
        _settings = settings.Value;
    }

    public async Task<Result<PayrollCalculationResult>> CalculateForEmployeeAsync(
        Guid employeeId,
        int taxYear,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default)
    {
        var salary = await _context.EmployeeSalaries
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId &&
                        s.EffectiveDate <= periodEnd &&
                        (s.EndDate == null || s.EndDate >= periodStart))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (salary is null)
        {
            return Result.Failure<PayrollCalculationResult>(
                $"No salary assignment effective for employee {employeeId}.");
        }

        var bands = await LoadTaxBandsAsync(taxYear, cancellationToken);
        if (bands.IsFailure)
        {
            return Result.Failure<PayrollCalculationResult>(bands.Error!);
        }

        var rates = await LoadStatutoryRatesAsync(taxYear, cancellationToken);
        if (rates.IsFailure)
        {
            return Result.Failure<PayrollCalculationResult>(rates.Error!);
        }

        var payFactor = await ComputePayFactorAsync(employeeId, periodStart, periodEnd, cancellationToken);

        var deductions = await _context.PayrollDeductions
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId &&
                        d.IsActive &&
                        d.StartDate <= periodEnd &&
                        (d.EndDate == null || d.EndDate >= periodStart))
            .ToListAsync(cancellationToken);

        var gross = salary.GrossSalary;
        var customDeductions = deductions
            .Select(d => new PayrollCustomDeductionInput(
                d.Name,
                d.FixedAmount ?? (d.PercentOfGross.HasValue ? gross * d.PercentOfGross.Value : 0m)))
            .ToList();

        var result = _calculator.Calculate(new PayrollCalculationRequest
        {
            Basic = salary.Basic,
            Housing = salary.Housing,
            Transport = salary.Transport,
            OtherAllowances = salary.OtherAllowances,
            PayFactor = payFactor,
            TaxBands = bands.Value!,
            StatutoryRates = rates.Value!,
            CustomDeductions = customDeductions,
        });

        return Result.Success(result);
    }

    internal async Task<decimal> ComputePayFactorAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var expectedWorkingDays = CountWorkingDays(periodStart, periodEnd);

        if (expectedWorkingDays <= 0)
        {
            expectedWorkingDays = _settings.DefaultWorkingDaysPerMonth;
        }

        var unpaidLeaveDays = await CountUnpaidLeaveDaysAsync(
            employeeId, periodStart, periodEnd, cancellationToken);

        var absentDays = await _context.AttendanceRecords
            .AsNoTracking()
            .CountAsync(r =>
                r.EmployeeId == employeeId &&
                r.Date >= periodStart &&
                r.Date <= periodEnd &&
                r.Status == AttendanceStatus.Absent,
                cancellationToken);

        return PayrollPayFactorCalculator.Calculate(expectedWorkingDays, unpaidLeaveDays, absentDays);
    }

    private async Task<int> CountUnpaidLeaveDaysAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var leaveRequests = await _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r =>
                r.EmployeeId == employeeId &&
                r.Status == LeaveRequestStatus.Approved &&
                r.StartDate <= periodEnd &&
                r.EndDate >= periodStart)
            .ToListAsync(cancellationToken);

        var days = 0;

        foreach (var request in leaveRequests.Where(r => !r.LeaveType.IsPaid))
        {
            var start = request.StartDate < periodStart ? periodStart : request.StartDate;
            var end = request.EndDate > periodEnd ? periodEnd : request.EndDate;

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    days++;
                }
            }
        }

        return days;
    }

    internal static int CountWorkingDays(DateOnly start, DateOnly end)
    {
        var count = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                count++;
            }
        }

        return count;
    }

    private async Task<Result<IReadOnlyList<PayrollTaxBandInput>>> LoadTaxBandsAsync(
        int taxYear,
        CancellationToken cancellationToken)
    {
        var bands = await _context.TaxBands
            .AsNoTracking()
            .Where(b => b.TaxYear == taxYear && b.IsActive)
            .OrderBy(b => b.BandOrder)
            .Select(b => new PayrollTaxBandInput(b.BandOrder, b.UpperBoundAnnual, b.Rate))
            .ToListAsync(cancellationToken);

        return bands.Count == 0
            ? Result.Failure<IReadOnlyList<PayrollTaxBandInput>>(
                $"No active tax bands configured for tax year {taxYear}.")
            : Result.Success<IReadOnlyList<PayrollTaxBandInput>>(bands);
    }

    private async Task<Result<IReadOnlyList<PayrollStatutoryRateInput>>> LoadStatutoryRatesAsync(
        int taxYear,
        CancellationToken cancellationToken)
    {
        var rates = await _context.PayrollStatutoryRates
            .AsNoTracking()
            .Where(r => r.TaxYear == taxYear && r.IsActive)
            .Select(r => new PayrollStatutoryRateInput(
                r.Code, r.EmployeeRate, r.EmployerRate,
                r.FixedAnnualAmount, r.VariableRate, r.Basis))
            .ToListAsync(cancellationToken);

        return rates.Count == 0
            ? Result.Failure<IReadOnlyList<PayrollStatutoryRateInput>>(
                $"No statutory rates configured for tax year {taxYear}.")
            : Result.Success<IReadOnlyList<PayrollStatutoryRateInput>>(rates);
    }

    public static string SerializeCustomDeductions(IReadOnlyList<PayrollCustomDeductionInput> items)
        => JsonSerializer.Serialize(items);
}
