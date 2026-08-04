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
// DeviceEventIngestion (FR-ATT-005)
// ---------------------------------------------------------------------------

/// <summary>Vendor-agnostic biometric clock event payload.</summary>
public sealed record DeviceEventPayload(
    string EmployeeCode,
    string EventType,
    DateTime TimestampUtc,
    string? DeviceId = null);

/// <summary>Ingests a clock event from a biometric device.</summary>
public sealed record IngestDeviceEventCommand(
    DeviceEventPayload Payload,
    string? ApiKey) : IRequest<Result<AttendanceRecordDto>>;

/// <summary>Input validation for <see cref="IngestDeviceEventCommand"/>.</summary>
public sealed class IngestDeviceEventValidator : AbstractValidator<IngestDeviceEventCommand>
{
    private static readonly HashSet<string> AllowedEvents =
        new(StringComparer.OrdinalIgnoreCase) { "ClockIn", "ClockOut" };

    public IngestDeviceEventValidator()
    {
        RuleFor(c => c.Payload.EmployeeCode).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Payload.EventType)
            .NotEmpty()
            .Must(e => AllowedEvents.Contains(e))
            .WithMessage("EventType must be ClockIn or ClockOut.");
        RuleFor(c => c.Payload.TimestampUtc).NotEmpty();
    }
}

/// <summary>Handles <see cref="IngestDeviceEventCommand"/>.</summary>
public sealed class IngestDeviceEventHandler
    : IRequestHandler<IngestDeviceEventCommand, Result<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAttendanceIntegrationService _integration;
    private readonly AttendanceSettings _settings;

    public IngestDeviceEventHandler(
        IApplicationDbContext context,
        IAttendanceIntegrationService integration,
        IOptions<AttendanceSettings> settings)
    {
        _context = context;
        _integration = integration;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<AttendanceRecordDto>> Handle(
        IngestDeviceEventCommand request,
        CancellationToken cancellationToken)
    {
        if (!ValidateApiKey(request.ApiKey))
        {
            return Result.Failure<AttendanceRecordDto>("Invalid device integration API key.");
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.EmployeeCode == request.Payload.EmployeeCode.Trim(),
                cancellationToken);

        if (employee is null)
        {
            return Result.Failure<AttendanceRecordDto>(
                $"Employee with code '{request.Payload.EmployeeCode}' was not found.");
        }

        var eventDate = DateOnly.FromDateTime(request.Payload.TimestampUtc.AddHours(1));
        var isClockIn = request.Payload.EventType.Equals("ClockIn", StringComparison.OrdinalIgnoreCase);

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(
                r => r.EmployeeId == employee.Id && r.Date == eventDate,
                cancellationToken);

        var isNew = record is null;

        if (isNew)
        {
            record = new AttendanceRecord
            {
                EmployeeId = employee.Id,
                Date = eventDate,
            };
            _context.AttendanceRecords.Add(record);
        }

        record.Source = AttendanceSource.Device;
        record.DeviceId = request.Payload.DeviceId?.Trim();

        var shift = await _integration.ResolveShiftAsync(employee.Id, cancellationToken);
        record.ShiftId = shift?.Id;

        if (isClockIn)
        {
            if (record.ClockIn is not null)
            {
                return Result.Failure<AttendanceRecordDto>("Employee has already clocked in for this date.");
            }

            record.ClockIn = DateTime.SpecifyKind(request.Payload.TimestampUtc, DateTimeKind.Utc);
        }
        else
        {
            if (record.ClockIn is null)
            {
                return Result.Failure<AttendanceRecordDto>("Employee must clock in before clocking out.");
            }

            if (record.ClockOut is not null)
            {
                return Result.Failure<AttendanceRecordDto>("Employee has already clocked out for this date.");
            }

            record.ClockOut = DateTime.SpecifyKind(request.Payload.TimestampUtc, DateTimeKind.Utc);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _integration.RecalculateDailyStatusAsync(employee.Id, eventDate, cancellationToken);

        record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstAsync(r => r.EmployeeId == employee.Id && r.Date == eventDate, cancellationToken);

        return Result.Success(ClockInHandler.Map(record));
    }

    private bool ValidateApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(_settings.DeviceIntegrationApiKey))
        {
            return true;
        }

        return string.Equals(apiKey, _settings.DeviceIntegrationApiKey, StringComparison.Ordinal);
    }
}
