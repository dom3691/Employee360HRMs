using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Employee360.Application.Features.Attendance;

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

/// <summary>Attendance record response.</summary>
public sealed record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    DateTime? ClockIn,
    DateTime? ClockOut,
    AttendanceStatus Status,
    AttendanceSource Source,
    Guid? ShiftId,
    string? SourceIp,
    string? DeviceId,
    string? CorrectionReason,
    int OvertimeMinutes);

// ---------------------------------------------------------------------------
// ClockIn (FR-ATT-001)
// ---------------------------------------------------------------------------

/// <summary>Clocks the current employee in for today.</summary>
public sealed record ClockInCommand(string? SourceIp = null) : IRequest<Result<AttendanceRecordDto>>;

/// <summary>Handles <see cref="ClockInCommand"/>.</summary>
public sealed class ClockInHandler : IRequestHandler<ClockInCommand, Result<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAttendanceIntegrationService _integration;
    private readonly AttendanceSettings _settings;

    public ClockInHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAttendanceIntegrationService integration,
        IOptions<AttendanceSettings> settings)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _integration = integration;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<AttendanceRecordDto>> Handle(
        ClockInCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<AttendanceRecordDto>("No employee record is linked to your account.");
        }

        var today = _clock.TodayWat;
        var utcNow = _clock.UtcNow;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == today, cancellationToken);

        if (record?.ClockIn is not null)
        {
            return Result.Failure<AttendanceRecordDto>("You have already clocked in today.");
        }

        var shift = await _integration.ResolveShiftAsync(employeeId.Value, cancellationToken);

        if (record is null)
        {
            record = new AttendanceRecord
            {
                EmployeeId = employeeId.Value,
                Date = today,
            };
            _context.AttendanceRecords.Add(record);
        }

        record.ClockIn = utcNow;
        record.Source = AttendanceSource.Web;
        record.SourceIp = request.SourceIp?.Trim();
        record.ShiftId = shift?.Id;

        await _context.SaveChangesAsync(cancellationToken);
        await _integration.RecalculateDailyStatusAsync(employeeId.Value, today, cancellationToken);

        record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstAsync(r => r.EmployeeId == employeeId && r.Date == today, cancellationToken);

        return Result.Success(Map(record));
    }

    internal static AttendanceRecordDto Map(AttendanceRecord record) =>
        new(
            record.Id,
            record.EmployeeId,
            record.Date,
            record.ClockIn,
            record.ClockOut,
            record.Status,
            record.Source,
            record.ShiftId,
            record.SourceIp,
            record.DeviceId,
            record.CorrectionReason,
            record.OvertimeMinutes);
}

// ---------------------------------------------------------------------------
// ClockOut (FR-ATT-001)
// ---------------------------------------------------------------------------

/// <summary>Clocks the current employee out for today.</summary>
public sealed record ClockOutCommand(string? SourceIp = null) : IRequest<Result<AttendanceRecordDto>>;

/// <summary>Handles <see cref="ClockOutCommand"/>.</summary>
public sealed class ClockOutHandler : IRequestHandler<ClockOutCommand, Result<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAttendanceIntegrationService _integration;

    public ClockOutHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAttendanceIntegrationService integration)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _integration = integration;
    }

    /// <inheritdoc />
    public async Task<Result<AttendanceRecordDto>> Handle(
        ClockOutCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<AttendanceRecordDto>("No employee record is linked to your account.");
        }

        var today = _clock.TodayWat;
        var utcNow = _clock.UtcNow;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == today, cancellationToken);

        if (record?.ClockIn is null)
        {
            return Result.Failure<AttendanceRecordDto>("You must clock in before clocking out.");
        }

        if (record.ClockOut is not null)
        {
            return Result.Failure<AttendanceRecordDto>("You have already clocked out today.");
        }

        record.ClockOut = utcNow;

        if (!string.IsNullOrWhiteSpace(request.SourceIp))
        {
            record.SourceIp = request.SourceIp.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _integration.RecalculateDailyStatusAsync(employeeId.Value, today, cancellationToken);

        record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstAsync(r => r.EmployeeId == employeeId && r.Date == today, cancellationToken);

        return Result.Success(ClockInHandler.Map(record));
    }
}
