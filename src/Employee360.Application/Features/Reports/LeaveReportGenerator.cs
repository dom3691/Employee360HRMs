using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Leave balance, utilization, and history reports (FR-RPT-004..006).</summary>
public static class LeaveReportGenerator
{
    public static async Task<ReportTable> GenerateBalanceAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var rows = await context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.Year == year)
            .OrderBy(b => b.Employee!.LastName)
            .ThenBy(b => b.LeaveType!.Name)
            .Select(b => new object?[]
            {
                b.Employee!.EmployeeCode,
                b.Employee.FirstName + " " + b.Employee.LastName,
                b.LeaveType!.Name,
                b.Year,
                b.Entitled,
                b.Used,
                b.Pending,
                b.CarriedForward,
                b.Entitled + b.CarriedForward - b.Used - b.Pending,
            })
            .ToListAsync(cancellationToken);

        return new ReportTable
        {
            Title = "Leave Balance Report",
            Headers =
            [
                "EmployeeCode", "EmployeeName", "LeaveType", "Year",
                "Entitled", "Used", "Pending", "CarriedForward", "Available",
            ],
            Rows = rows.Cast<IReadOnlyList<object?>>().ToList(),
            Summary = new { Year = year, TotalRecords = rows.Count },
        };
    }

    public static async Task<ReportTable> GenerateUtilizationAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var balances = await context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.Year == year)
            .Select(b => new
            {
                b.Employee!.EmployeeCode,
                EmployeeName = b.Employee.FirstName + " " + b.Employee.LastName,
                LeaveType = b.LeaveType!.Name,
                b.Entitled,
                b.Used,
            })
            .ToListAsync(cancellationToken);

        var rows = balances
            .Select(b =>
            {
                var utilization = b.Entitled > 0
                    ? Math.Round(b.Used / b.Entitled * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                return new object?[]
                {
                    b.EmployeeCode,
                    b.EmployeeName,
                    b.LeaveType,
                    b.Entitled,
                    b.Used,
                    utilization,
                };
            })
            .Cast<IReadOnlyList<object?>>()
            .ToList();

        var avgUtilization = rows.Count > 0
            ? Math.Round(rows.Average(r => (decimal)r[5]!), 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new ReportTable
        {
            Title = "Leave Utilization Report",
            Headers = ["EmployeeCode", "EmployeeName", "LeaveType", "Entitled", "Used", "UtilizationPercent"],
            Rows = rows,
            Summary = new { Year = year, AverageUtilizationPercent = avgUtilization },
        };
    }

    public static async Task<ReportTable> GenerateHistoryAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var rows = await context.LeaveRequests
            .AsNoTracking()
            .Where(r =>
                r.Status == LeaveRequestStatus.Approved &&
                r.StartDate <= range.ToDate &&
                r.EndDate >= range.FromDate)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new object?[]
            {
                r.Employee!.EmployeeCode,
                r.Employee.FirstName + " " + r.Employee.LastName,
                r.LeaveType!.Name,
                r.StartDate,
                r.EndDate,
                r.Days,
                r.Status.ToString(),
                r.Reason,
            })
            .ToListAsync(cancellationToken);

        return new ReportTable
        {
            Title = "Leave History Report",
            Headers =
            [
                "EmployeeCode", "EmployeeName", "LeaveType",
                "StartDate", "EndDate", "Days", "Status", "Reason",
            ],
            Rows = rows.Cast<IReadOnlyList<object?>>().ToList(),
            Summary = new
            {
                range.FromDate,
                range.ToDate,
                TotalRequests = rows.Count,
                TotalDays = rows.Sum(r => (decimal)r[5]!),
            },
        };
    }
}
