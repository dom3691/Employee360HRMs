using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Services;
using Employee360.Application.Features.Payroll.Calculate;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire job that calculates payroll for all active employees (FR-PAY-008).
/// Supports progress tracking, idempotent retry, and rollback on failure.
/// </summary>
public sealed class CalculatePayrollJob
{
    private readonly Employee360DbContext _context;
    private readonly IEmployeePayrollCalculationService _calculationService;
    private readonly ILogger<CalculatePayrollJob> _logger;

    public CalculatePayrollJob(
        Employee360DbContext context,
        IEmployeePayrollCalculationService calculationService,
        ILogger<CalculatePayrollJob> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    /// <summary>Calculates payslips for every active employee with a salary assignment.</summary>
    public async Task RunAsync(Guid payrollRunId, CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Payslips)
            .FirstOrDefaultAsync(r => r.Id == payrollRunId, cancellationToken);

        if (run is null)
        {
            _logger.LogWarning("Payroll run {RunId} not found", payrollRunId);
            return;
        }

        if (run.Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated))
        {
            _logger.LogInformation(
                "Skipping calculation for run {RunId} — status is {Status}",
                payrollRunId,
                run.Status);
            return;
        }

        // Idempotent retry: wipe partial results.
        if (run.Payslips.Count > 0)
        {
            _context.Payslips.RemoveRange(run.Payslips);
            run.ProcessedEmployees = 0;
            await _context.SaveChangesAsync(cancellationToken);
        }

        run.Status = PayrollRunStatus.Draft;
        run.LastError = null;

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave) &&
                _context.EmployeeSalaries.Any(s =>
                    s.EmployeeId == e.Id &&
                    s.EffectiveDate <= run.PeriodEnd &&
                    (s.EndDate == null || s.EndDate >= run.PeriodStart)))
            .Select(e => new { e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.MiddleName })
            .ToListAsync(cancellationToken);

        run.TotalEmployees = employees.Count;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            foreach (var employee in employees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var calc = await _calculationService.CalculateForEmployeeAsync(
                    employee.Id,
                    run.TaxYear,
                    run.PeriodStart,
                    run.PeriodEnd,
                    cancellationToken);

                if (calc.IsFailure)
                {
                    throw new InvalidOperationException(
                        $"Calculation failed for {employee.EmployeeCode}: {calc.Error}");
                }

                var result = calc.Value!;
                var name = string.Join(' ',
                    new[] { employee.FirstName, employee.MiddleName, employee.LastName }
                        .Where(p => !string.IsNullOrWhiteSpace(p)));

                _context.Payslips.Add(new Payslip
                {
                    PayrollRunId = run.Id,
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.EmployeeCode,
                    EmployeeName = name,
                    Basic = result.Basic,
                    Housing = result.Housing,
                    Transport = result.Transport,
                    OtherAllowances = result.OtherAllowances,
                    GrossPay = result.GrossPay,
                    PayFactor = result.PayFactor,
                    CraMonthly = result.CraMonthly,
                    Paye = result.Paye,
                    PensionEmployee = result.PensionEmployee,
                    PensionEmployer = result.PensionEmployer,
                    Nhf = result.Nhf,
                    NsitfEmployer = result.NsitfEmployer,
                    CustomDeductionsTotal = result.CustomDeductionsTotal,
                    NetPay = result.NetPay,
                    CustomDeductionsJson = EmployeePayrollCalculationService.SerializeCustomDeductions(
                        result.CustomDeductions),
                });

                run.ProcessedEmployees++;
                await _context.SaveChangesAsync(cancellationToken);
            }

            run.Status = PayrollRunStatus.Calculated;
            run.LastError = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payroll calculation complete for run {RunId}: {Count} employees",
                payrollRunId,
                employees.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payroll calculation failed for run {RunId}", payrollRunId);

            var payslips = await _context.Payslips
                .Where(p => p.PayrollRunId == run.Id)
                .ToListAsync(cancellationToken);

            _context.Payslips.RemoveRange(payslips);

            run.Status = PayrollRunStatus.Draft;
            run.ProcessedEmployees = 0;
            run.LastError = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}
