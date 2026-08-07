using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Attendance summary report (FR-RPT-007).</summary>
public static class AttendanceReportGenerator
{
    public static async Task<ReportTable> GenerateSummaryAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var records = await context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.Date >= range.FromDate && r.Date <= range.ToDate)
            .Select(r => new
            {
                r.Employee!.EmployeeCode,
                Name = r.Employee.FirstName + " " + r.Employee.LastName,
                r.Status,
            })
            .ToListAsync(cancellationToken);

        var grouped = records
            .GroupBy(r => new { r.EmployeeCode, r.Name })
            .Select(g => new
            {
                g.Key.EmployeeCode,
                g.Key.Name,
                Present = g.Count(r => r.Status == AttendanceStatus.Present),
                Late = g.Count(r => r.Status == AttendanceStatus.Late),
                Absent = g.Count(r => r.Status == AttendanceStatus.Absent),
                HalfDay = g.Count(r => r.Status == AttendanceStatus.HalfDay),
                OnLeave = g.Count(r => r.Status == AttendanceStatus.OnLeave),
                Holiday = g.Count(r => r.Status == AttendanceStatus.Holiday),
                Total = g.Count(),
            })
            .OrderBy(x => x.Name)
            .ToList();

        var rows = grouped
            .Select(r => new object?[]
            {
                r.EmployeeCode, r.Name, r.Present, r.Late, r.Absent,
                r.HalfDay, r.OnLeave, r.Holiday, r.Total,
            })
            .Cast<IReadOnlyList<object?>>()
            .ToList();

        return new ReportTable
        {
            Title = "Attendance Summary",
            Headers =
            [
                "EmployeeCode", "EmployeeName", "Present", "Late", "Absent",
                "HalfDay", "OnLeave", "Holiday", "TotalDays",
            ],
            Rows = rows,
            Summary = new
            {
                range.FromDate,
                range.ToDate,
                TotalPresent = grouped.Sum(r => r.Present),
                TotalAbsent = grouped.Sum(r => r.Absent),
                TotalLate = grouped.Sum(r => r.Late),
            },
        };
    }
}
