using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Reports;

/// <summary>Shared helpers for report date/headcount logic.</summary>
public static class ReportQueryHelpers
{
    public static bool IsActiveHeadcount(EmployeeStatus status) =>
        status is EmployeeStatus.Active or EmployeeStatus.OnLeave or EmployeeStatus.Suspended;

    public static bool WasEmployedOn(
        DateOnly? joinDate,
        EmployeeStatus status,
        DateTime? modifiedAtUtc,
        DateOnly asOfDate)
    {
        if (joinDate.HasValue && joinDate.Value > asOfDate)
        {
            return false;
        }

        if (IsActiveHeadcount(status))
        {
            return true;
        }

        if (ReportAttritionCalculator.IsExitStatus(status))
        {
            if (!modifiedAtUtc.HasValue)
            {
                return false;
            }

            var exitDate = DateOnly.FromDateTime(modifiedAtUtc.Value);
            return exitDate > asOfDate;
        }

        return false;
    }

    public static IQueryable<Domain.Entities.Employee> ActiveEmployees(IApplicationDbContext context) =>
        context.Employees.AsNoTracking().Where(e => IsActiveHeadcount(e.Status));
}
