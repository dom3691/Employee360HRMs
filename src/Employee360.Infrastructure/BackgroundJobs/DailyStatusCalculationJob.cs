using Employee360.Application.Common.Services;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring job that finalizes yesterday's attendance status for all active
/// employees (FR-ATT-003, FR-ATT-008): holidays, approved leave, late/present/
/// half-day/absent based on shift rules.
/// </summary>
public sealed class DailyStatusCalculationJob
{
    private readonly Employee360DbContext _context;
    private readonly IAttendanceIntegrationService _attendanceIntegration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DailyStatusCalculationJob> _logger;

    public DailyStatusCalculationJob(
        Employee360DbContext context,
        IAttendanceIntegrationService attendanceIntegration,
        IDateTimeProvider dateTimeProvider,
        ILogger<DailyStatusCalculationJob> logger)
    {
        _context = context;
        _attendanceIntegration = attendanceIntegration;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <summary>Recalculates attendance for the previous business date.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var targetDate = _dateTimeProvider.TodayWat.AddDays(-1);

        var employeeIds = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        foreach (var employeeId in employeeIds)
        {
            await _attendanceIntegration.RecalculateDailyStatusAsync(
                employeeId, targetDate, cancellationToken);
        }

        _logger.LogInformation(
            "Daily attendance status calculation complete for {Date}: {EmployeeCount} employees processed",
            targetDate,
            employeeIds.Count);
    }
}
