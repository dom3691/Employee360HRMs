using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Attrition report (FR-RPT-003).</summary>
public static class AttritionReportGenerator
{
    public static async Task<ReportTable> GenerateAsync(
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
                e.EmployeeCode,
                e.FirstName,
                e.LastName,
                Department = e.Department != null ? e.Department.Name : "Unassigned",
            })
            .ToListAsync(cancellationToken);

        var opening = employees.Count(e =>
            ReportQueryHelpers.WasEmployedOn(e.JoinDate, e.Status, e.ModifiedAt, range.FromDate.AddDays(-1)));

        var closing = employees.Count(e =>
            ReportQueryHelpers.WasEmployedOn(e.JoinDate, e.Status, e.ModifiedAt, range.ToDate));

        var exits = employees
            .Where(e =>
                ReportAttritionCalculator.IsExitStatus(e.Status) &&
                e.ModifiedAt.HasValue &&
                DateOnly.FromDateTime(e.ModifiedAt.Value) >= range.FromDate &&
                DateOnly.FromDateTime(e.ModifiedAt.Value) <= range.ToDate)
            .ToList();

        var voluntary = exits.Count(e => ReportAttritionCalculator.IsVoluntary(e.Status));
        var involuntary = exits.Count(e => e.Status == EmployeeStatus.Terminated);

        var metrics = ReportAttritionCalculator.Calculate(opening, closing, voluntary, involuntary);

        var rows = exits
            .OrderBy(e => e.ModifiedAt)
            .Select(e => new object?[]
            {
                e.EmployeeCode,
                $"{e.FirstName} {e.LastName}",
                e.Department,
                ReportAttritionCalculator.IsVoluntary(e.Status) ? "Voluntary" : "Involuntary",
                e.Status.ToString(),
                e.ModifiedAt.HasValue ? DateOnly.FromDateTime(e.ModifiedAt.Value) : null,
            })
            .Cast<IReadOnlyList<object?>>()
            .ToList();

        return new ReportTable
        {
            Title = "Attrition Report",
            Headers =
            [
                "EmployeeCode", "EmployeeName", "Department",
                "ExitType", "Status", "ExitDate",
            ],
            Rows = rows,
            Summary = metrics,
        };
    }
}
