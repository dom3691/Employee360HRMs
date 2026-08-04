using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Employee360.Application.Common.Models;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Integrates approved leave and holidays into attendance records (FR-ATT-008)
/// and recalculates daily status for existing rows.
/// </summary>
public interface IAttendanceIntegrationService
{
    /// <summary>
    /// Creates or updates attendance rows for each day in an approved leave range.
    /// </summary>
    Task SyncApprovedLeaveAsync(LeaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Recalculates status for one employee on one business date.</summary>
    Task RecalculateDailyStatusAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves the shift assigned to the employee's department.</summary>
    Task<Shift?> ResolveShiftAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class AttendanceIntegrationService : IAttendanceIntegrationService
{
    private readonly IApplicationDbContext _context;
    private readonly AttendanceSettings _settings;

    public AttendanceIntegrationService(
        IApplicationDbContext context,
        IOptions<AttendanceSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task SyncApprovedLeaveAsync(
        LeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Status != LeaveRequestStatus.Approved)
        {
            return;
        }

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            if (await IsPublicHolidayAsync(date, cancellationToken))
            {
                continue;
            }

            var record = await GetOrCreateRecordAsync(request.EmployeeId, date, cancellationToken);

            record.Status = AttendanceStatus.OnLeave;
            record.ClockIn = null;
            record.ClockOut = null;
            record.Source = AttendanceSource.Manual;
            record.CorrectionReason = $"Approved leave ({request.Id})";
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecalculateDailyStatusAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var shift = await ResolveShiftAsync(employeeId, cancellationToken);
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date, cancellationToken);

        var isHoliday = await IsPublicHolidayAsync(date, cancellationToken);
        var isOnLeave = await IsOnApprovedLeaveAsync(employeeId, date, cancellationToken);

        if (record is null)
        {
            if (isHoliday)
            {
                record = await GetOrCreateRecordAsync(employeeId, date, cancellationToken);
                record.Status = AttendanceStatus.Holiday;
                record.ShiftId = shift?.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }
            else if (isOnLeave)
            {
                record = await GetOrCreateRecordAsync(employeeId, date, cancellationToken);
                record.Status = AttendanceStatus.OnLeave;
                record.ShiftId = shift?.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        record.ShiftId = shift?.Id;

        var evaluationEnd = shift is not null
            ? AttendanceStatusCalculator.ToUtc(date, shift.EndTime)
            : (DateTime?)null;

        record.Status = AttendanceStatusCalculator.CalculateStatus(
            record.ClockIn,
            record.ClockOut,
            shift,
            date,
            isHoliday,
            isOnLeave,
            _settings.HalfDayThresholdPercent,
            evaluationEnd);

        if (shift is not null)
        {
            record.OvertimeMinutes = AttendanceStatusCalculator.CalculateOvertimeMinutes(
                record.ClockIn,
                record.ClockOut,
                shift,
                date,
                _settings.OvertimeThresholdMinutes);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Shift?> ResolveShiftAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.Department!.Shift)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken cancellationToken)
        => await _context.PublicHolidays.AnyAsync(h => h.Date == date, cancellationToken);

    private async Task<bool> IsOnApprovedLeaveAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
        => await _context.LeaveRequests.AnyAsync(
            r => r.EmployeeId == employeeId &&
                 r.Status == LeaveRequestStatus.Approved &&
                 r.StartDate <= date &&
                 r.EndDate >= date,
            cancellationToken);

    private async Task<AttendanceRecord> GetOrCreateRecordAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == date, cancellationToken);

        if (record is not null)
        {
            return record;
        }

        record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = date,
        };

        _context.AttendanceRecords.Add(record);
        return record;
    }
}
