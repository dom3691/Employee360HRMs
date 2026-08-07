using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Headcount summary and trend reports (FR-RPT-001/002).</summary>
public static class HeadcountReportGenerator
{
    public static async Task<ReportTable> GenerateSummaryAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var employees = await ReportQueryHelpers.ActiveEmployees(context)
            .Include(e => e.Department)
            .ToListAsync(cancellationToken);

        var byDepartment = employees
            .GroupBy(e => e.Department?.Name ?? "Unassigned")
            .OrderBy(g => g.Key)
            .Select(g => new object?[] { g.Key, g.Count() })
            .ToList();

        var byGender = employees
            .GroupBy(e => e.Gender)
            .OrderBy(g => g.Key)
            .Select(g => new object?[] { g.Key.ToString(), g.Count() })
            .ToList();

        var byType = employees
            .GroupBy(e => e.EmploymentType)
            .OrderBy(g => g.Key)
            .Select(g => new object?[] { g.Key.ToString(), g.Count() })
            .ToList();

        var rows = new List<IReadOnlyList<object?>>();
        rows.AddRange(byDepartment.Select(r => new object?[] { "Department", r[0], r[1] }));
        rows.AddRange(byGender.Select(r => new object?[] { "Gender", r[0], r[1] }));
        rows.AddRange(byType.Select(r => new object?[] { "EmploymentType", r[0], r[1] }));

        return new ReportTable
        {
            Title = "Headcount Summary",
            Headers = ["Category", "Segment", "Count"],
            Rows = rows,
            Summary = new
            {
                TotalHeadcount = employees.Count,
                AsOfDate = range.ToDate,
                ByDepartment = byDepartment.ToDictionary(r => r[0]!.ToString()!, r => r[1]),
                ByGender = byGender.ToDictionary(r => r[0]!.ToString()!, r => r[1]),
                ByEmploymentType = byType.ToDictionary(r => r[0]!.ToString()!, r => r[1]),
            },
        };
    }

    public static async Task<ReportTable> GenerateTrendAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var employees = await context.Employees
            .AsNoTracking()
            .Select(e => new
            {
                e.JoinDate,
                e.Status,
                e.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        var rows = new List<IReadOnlyList<object?>>();

        foreach (var month in range.Months())
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            if (monthEnd > range.ToDate)
            {
                monthEnd = range.ToDate;
            }

            var count = employees.Count(e => ReportQueryHelpers.WasEmployedOn(e.JoinDate, e.Status, e.ModifiedAt, monthEnd));
            rows.Add([month.ToString("yyyy-MM"), count]);
        }

        return new ReportTable
        {
            Title = "Headcount Trend",
            Headers = ["Month", "Headcount"],
            Rows = rows,
            Summary = new
            {
                range.FromDate,
                range.ToDate,
                Trend = rows.Select(r => new { Month = r[0], Headcount = r[1] }).ToList(),
            },
        };
    }
}
