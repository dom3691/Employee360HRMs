using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Executive, HR, and Payroll dashboard aggregations (FR-RPT).</summary>
public static class DashboardReportGenerator
{
    public static async Task<ReportTable> GenerateExecutiveAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var headcountTrend = await HeadcountReportGenerator.GenerateTrendAsync(context, range, cancellationToken);
        var attrition = await AttritionReportGenerator.GenerateAsync(context, range, cancellationToken);
        var payrollCost = await PayrollReportGenerator.GenerateCostByDepartmentAsync(context, range, cancellationToken);

        var employees = await ReportQueryHelpers.ActiveEmployees(context)
            .Select(e => e.Gender)
            .ToListAsync(cancellationToken);

        var total = employees.Count;
        var genderBreakdown = employees
            .GroupBy(g => g)
            .ToDictionary(g => g.Key.ToString(), g => new
            {
                Count = g.Count(),
                Percent = total > 0
                    ? Math.Round(g.Count() / (decimal)total * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m,
            });

        var summary = new
        {
            HeadcountTrend = headcountTrend.Summary,
            Attrition = attrition.Summary,
            PayrollCostByDepartment = payrollCost.Summary,
            GenderDiversity = genderBreakdown,
            CurrentHeadcount = total,
        };

        var rows = headcountTrend.Rows
            .Select(r => new object?[] { "Headcount", r[0], r[1] })
            .Concat(payrollCost.Rows.Select(r => new object?[] { "PayrollCost", r[0], r[2], r[3] }))
            .ToList();

        return new ReportTable
        {
            Title = "Executive Dashboard",
            Headers = ["Widget", "Segment", "Value1", "Value2"],
            Rows = rows,
            Summary = summary,
        };
    }

    public static async Task<ReportTable> GenerateHrAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        int year,
        CancellationToken cancellationToken)
    {
        var headcountTotal = await ReportQueryHelpers.ActiveEmployees(context).CountAsync(cancellationToken);
        var leaveUtil = await LeaveReportGenerator.GenerateUtilizationAsync(context, year, cancellationToken);
        var attendance = await AttendanceReportGenerator.GenerateSummaryAsync(context, range, cancellationToken);

        var pendingLeave = await context.LeaveRequests
            .AsNoTracking()
            .CountAsync(r => r.Status == LeaveRequestStatus.Pending, cancellationToken);

        var avgUtil = leaveUtil.Rows.Count > 0
            ? Math.Round(leaveUtil.Rows.Average(r => Convert.ToDecimal(r[5])), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var summary = new
        {
            ActiveHeadcount = headcountTotal,
            AverageLeaveUtilizationPercent = avgUtil,
            AttendanceSummary = attendance.Summary,
            PendingLeaveRequests = pendingLeave,
        };

        return new ReportTable
        {
            Title = "HR Dashboard",
            Headers = ["Metric", "Value"],
            Rows =
            [
                ["ActiveHeadcount", headcountTotal],
                ["AverageLeaveUtilizationPercent", avgUtil],
                ["PendingLeaveRequests", pendingLeave],
            ],
            Summary = summary,
        };
    }

    public static async Task<ReportTable> GeneratePayrollAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var latestRun = await context.PayrollRuns
            .AsNoTracking()
            .OrderByDescending(r => r.PeriodYear)
            .ThenByDescending(r => r.PeriodMonth)
            .FirstOrDefaultAsync(cancellationToken);

        var register = await PayrollReportGenerator.GenerateRegisterAsync(context, range, cancellationToken);
        var paye = await PayrollReportGenerator.GeneratePayeScheduleAsync(context, range, cancellationToken);
        var pension = await PayrollReportGenerator.GeneratePensionScheduleAsync(context, range, cancellationToken);
        var nhf = await PayrollReportGenerator.GenerateNhfScheduleAsync(context, range, cancellationToken);

        var totalNet = register.Rows.Sum(r => Convert.ToDecimal(r[7]));
        var totalPaye = paye.Rows.Sum(r => Convert.ToDecimal(r[3]));
        var totalPension = pension.Rows.Sum(r => Convert.ToDecimal(r[3]));
        var totalNhf = nhf.Rows.Sum(r => Convert.ToDecimal(r[3]));

        var summary = new
        {
            LatestPayrollRun = latestRun is null
                ? null
                : new
                {
                    latestRun.Id,
                    latestRun.PeriodLabel,
                    latestRun.Status,
                    latestRun.FinalizedAtUtc,
                },
            TotalNetPayroll = totalNet,
            TotalPaye = totalPaye,
            TotalPension = totalPension,
            TotalNhf = totalNhf,
        };

        return new ReportTable
        {
            Title = "Payroll Dashboard",
            Headers = ["Metric", "Value"],
            Rows =
            [
                ["TotalNetPayroll", totalNet],
                ["TotalPAYE", totalPaye],
                ["TotalPension", totalPension],
                ["TotalNHF", totalNhf],
            ],
            Summary = summary,
        };
    }
}
