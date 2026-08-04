using Employee360.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Employee master list and birthday/anniversary reports (FR-RPT-014/015).</summary>
public static class EmployeeReportGenerator
{
    public static async Task<ReportTable> GenerateMasterListAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var rows = await ReportQueryHelpers.ActiveEmployees(context)
            .Include(e => e.Department)
            .Include(e => e.Position)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new object?[]
            {
                e.EmployeeCode,
                e.FirstName + " " + e.LastName,
                e.Email,
                e.Department != null ? e.Department.Name : "Unassigned",
                e.Position != null ? e.Position.Title : string.Empty,
                e.EmploymentType.ToString(),
                e.JoinDate,
                e.Status.ToString(),
            })
            .ToListAsync(cancellationToken);

        return new ReportTable
        {
            Title = "Employee Master List",
            Headers =
            [
                "EmployeeCode", "EmployeeName", "Email", "Department",
                "Position", "EmploymentType", "JoinDate", "Status",
            ],
            Rows = rows.Cast<IReadOnlyList<object?>>().ToList(),
            Summary = new { TotalEmployees = rows.Count },
        };
    }

    public static async Task<ReportTable> GenerateBirthdayAnniversaryAsync(
        IApplicationDbContext context,
        ReportDateRange range,
        CancellationToken cancellationToken)
    {
        var employees = await ReportQueryHelpers.ActiveEmployees(context)
            .Where(e => e.DateOfBirth != null || e.JoinDate != null)
            .Select(e => new
            {
                e.EmployeeCode,
                Name = e.FirstName + " " + e.LastName,
                e.DateOfBirth,
                e.JoinDate,
            })
            .ToListAsync(cancellationToken);

        var rows = new List<IReadOnlyList<object?>>();

        foreach (var employee in employees)
        {
            if (employee.DateOfBirth.HasValue &&
                IsOccasionInRange(employee.DateOfBirth.Value, range))
            {
                rows.Add(
                [
                    employee.EmployeeCode,
                    employee.Name,
                    "Birthday",
                    NextOccurrence(employee.DateOfBirth.Value, range.FromDate),
                    null,
                ]);
            }

            if (employee.JoinDate.HasValue &&
                IsOccasionInRange(employee.JoinDate.Value, range))
            {
                rows.Add(
                [
                    employee.EmployeeCode,
                    employee.Name,
                    "Work Anniversary",
                    NextOccurrence(employee.JoinDate.Value, range.FromDate),
                    YearsOfService(employee.JoinDate.Value, range.ToDate),
                ]);
            }
        }

        rows = rows.OrderBy(r => r[3]).ToList();

        return new ReportTable
        {
            Title = "Birthday & Anniversary Report",
            Headers = ["EmployeeCode", "EmployeeName", "EventType", "EventDate", "YearsOfService"],
            Rows = rows,
            Summary = new
            {
                range.FromDate,
                range.ToDate,
                TotalEvents = rows.Count,
            },
        };
    }

    public static bool IsOccasionInRange(DateOnly occasion, ReportDateRange range)
    {
        for (var year = range.FromDate.Year; year <= range.ToDate.Year; year++)
        {
            var day = SafeDate(year, occasion.Month, occasion.Day);
            if (day >= range.FromDate && day <= range.ToDate)
            {
                return true;
            }
        }

        return false;
    }

    internal static DateOnly NextOccurrence(DateOnly occasion, DateOnly fromDate)
    {
        var candidate = SafeDate(fromDate.Year, occasion.Month, occasion.Day);
        if (candidate < fromDate)
        {
            candidate = SafeDate(fromDate.Year + 1, occasion.Month, occasion.Day);
        }

        return candidate;
    }

    internal static int YearsOfService(DateOnly joinDate, DateOnly asOfDate)
    {
        var years = asOfDate.Year - joinDate.Year;
        if (SafeDate(asOfDate.Year, joinDate.Month, joinDate.Day) > asOfDate)
        {
            years--;
        }

        return Math.Max(0, years);
    }

    private static DateOnly SafeDate(int year, int month, int day)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, daysInMonth));
    }
}
