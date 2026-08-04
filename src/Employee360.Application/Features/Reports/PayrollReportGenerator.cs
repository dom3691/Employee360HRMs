using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Payroll register, cost, and statutory schedule reports (FR-RPT-008..012).</summary>
public static class PayrollReportGenerator
{
    public static async Task<ReportTable> GenerateRegisterAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var rows = await QueryFinalizedPayslips(context, range)
            .OrderBy(p => p.PayrollRun!.PeriodYear)
            .ThenBy(p => p.PayrollRun!.PeriodMonth)
            .ThenBy(p => p.EmployeeName)
            .Select(p => new object?[]
            {
                p.PayrollRun!.PeriodLabel,
                p.EmployeeCode,
                p.EmployeeName,
                p.GrossPay,
                p.Paye,
                p.PensionEmployee,
                p.Nhf,
                p.NetPay,
            })
            .ToListAsync(cancellationToken);

        return new ReportTable
        {
            Title = "Payroll Register",
            Headers =
            [
                "Period", "EmployeeCode", "EmployeeName",
                "GrossPay", "PAYE", "Pension", "NHF", "NetPay",
            ],
            Rows = rows.Cast<IReadOnlyList<object?>>().ToList(),
            Summary = new
            {
                range.FromDate,
                range.ToDate,
                TotalGross = rows.Sum(r => (decimal)r[3]!),
                TotalNet = rows.Sum(r => (decimal)r[7]!),
            },
        };
    }

    public static async Task<ReportTable> GenerateCostByDepartmentAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var payslips = await QueryFinalizedPayslips(context, range)
            .Select(p => new
            {
                Department = p.Employee!.Department != null ? p.Employee.Department.Name : "Unassigned",
                p.GrossPay,
                p.NetPay,
            })
            .ToListAsync(cancellationToken);

        var rows = payslips
            .GroupBy(p => p.Department)
            .OrderBy(g => g.Key)
            .Select(g => new object?[]
            {
                g.Key,
                g.Count(),
                g.Sum(x => x.GrossPay),
                g.Sum(x => x.NetPay),
            })
            .Cast<IReadOnlyList<object?>>()
            .ToList();

        return new ReportTable
        {
            Title = "Payroll Cost by Department",
            Headers = ["Department", "EmployeeCount", "TotalGrossPay", "TotalNetPay"],
            Rows = rows,
            Summary = new
            {
                TotalGross = payslips.Sum(p => p.GrossPay),
                TotalNet = payslips.Sum(p => p.NetPay),
            },
        };
    }

    public static Task<ReportTable> GeneratePayeScheduleAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
        => GenerateStatutoryScheduleAsync(context, range, "PAYE", p => p.Paye, cancellationToken);

    public static Task<ReportTable> GeneratePensionScheduleAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
        => GenerateStatutoryScheduleAsync(
            context, range, "Pension",
            p => p.PensionEmployee + p.PensionEmployer,
            cancellationToken);

    public static Task<ReportTable> GenerateNhfScheduleAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
        => GenerateStatutoryScheduleAsync(context, range, "NHF", p => p.Nhf, cancellationToken);

    private static async Task<ReportTable> GenerateStatutoryScheduleAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        string scheduleType,
        Func<Domain.Entities.Payslip, decimal> amountSelector,
        CancellationToken cancellationToken)
    {
        var payslips = await QueryFinalizedPayslips(context, range)
            .OrderBy(p => p.PayrollRun!.PeriodLabel)
            .ThenBy(p => p.EmployeeName)
            .ToListAsync(cancellationToken);

        var rows = payslips
            .Select(p => new object?[]
            {
                p.PayrollRun!.PeriodLabel,
                p.EmployeeCode,
                p.EmployeeName,
                amountSelector(p),
            })
            .Cast<IReadOnlyList<object?>>()
            .ToList();

        return new ReportTable
        {
            Title = $"{scheduleType} Remittance Schedule",
            Headers = ["Period", "EmployeeCode", "EmployeeName", "Amount"],
            Rows = rows,
            Summary = new
            {
                ScheduleType = scheduleType,
                range.FromDate,
                range.ToDate,
                TotalAmount = payslips.Sum(amountSelector),
            },
        };
    }

    private static IQueryable<Domain.Entities.Payslip> QueryFinalizedPayslips(
        IApplicationDbContext context,
        ReportDateRange range)
    {
        return context.Payslips
            .AsNoTracking()
            .Include(p => p.PayrollRun)
            .Include(p => p.Employee!)
            .ThenInclude(e => e.Department)
            .Where(p =>
                p.PayrollRun!.Status == PayrollRunStatus.Finalized &&
                p.PayrollRun.PeriodStart <= range.ToDate &&
                p.PayrollRun.PeriodEnd >= range.FromDate);
    }
}
