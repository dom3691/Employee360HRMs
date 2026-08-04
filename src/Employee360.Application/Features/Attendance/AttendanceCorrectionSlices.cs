using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Services;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Employee360.Application.Features.Attendance;

// ---------------------------------------------------------------------------
// ManualAttendanceCorrection (FR-ATT-006)
// ---------------------------------------------------------------------------

/// <summary>HR/Manager manual attendance correction with mandatory reason.</summary>
public sealed record ManualAttendanceCorrectionCommand(
    Guid EmployeeId,
    DateOnly Date,
    DateTime? ClockIn,
    DateTime? ClockOut,
    AttendanceStatus? Status,
    string Reason) : IRequest<Result<AttendanceRecordDto>>;

/// <summary>Input validation for <see cref="ManualAttendanceCorrectionCommand"/>.</summary>
public sealed class ManualAttendanceCorrectionValidator : AbstractValidator<ManualAttendanceCorrectionCommand>
{
    public ManualAttendanceCorrectionValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Date).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(512);
    }
}

/// <summary>Handles <see cref="ManualAttendanceCorrectionCommand"/>.</summary>
public sealed class ManualAttendanceCorrectionHandler
    : IRequestHandler<ManualAttendanceCorrectionCommand, Result<AttendanceRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IManagerScopeService _managerScope;
    private readonly IAttendanceIntegrationService _integration;
    private readonly IDateTimeProvider _clock;

    public ManualAttendanceCorrectionHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IManagerScopeService managerScope,
        IAttendanceIntegrationService integration,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _managerScope = managerScope;
        _integration = integration;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<AttendanceRecordDto>> Handle(
        ManualAttendanceCorrectionCommand request,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeAsync(request.EmployeeId, cancellationToken);

        if (authorization.IsFailure)
        {
            return Result.Failure<AttendanceRecordDto>(authorization.Error!);
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<AttendanceRecordDto>("Employee not found.");
        }

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(
                r => r.EmployeeId == request.EmployeeId && r.Date == request.Date,
                cancellationToken);

        var oldSnapshot = record is null
            ? "{}"
            : $$"""{"clockIn":"{{record.ClockIn}}","clockOut":"{{record.ClockOut}}","status":"{{record.Status}}"}""";

        var isNew = record is null;

        if (isNew)
        {
            record = new AttendanceRecord
            {
                EmployeeId = request.EmployeeId,
                Date = request.Date,
            };
            _context.AttendanceRecords.Add(record);
        }

        record.ClockIn = request.ClockIn;
        record.ClockOut = request.ClockOut;
        record.Source = AttendanceSource.Manual;
        record.CorrectionReason = request.Reason.Trim();

        var shift = await _integration.ResolveShiftAsync(request.EmployeeId, cancellationToken);
        record.ShiftId = shift?.Id;

        if (request.Status.HasValue)
        {
            record.Status = request.Status.Value;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id.ToString(),
            Action = "ManualCorrection",
            OldValues = oldSnapshot,
            NewValues = $$"""{"clockIn":"{{record.ClockIn}}","clockOut":"{{record.ClockOut}}","status":"{{record.Status}}","reason":"{{request.Reason.Replace("\"", "'")}}"}""",
            UserId = _currentUser.UserId,
            Timestamp = _clock.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        if (!request.Status.HasValue)
        {
            await _integration.RecalculateDailyStatusAsync(
                request.EmployeeId, request.Date, cancellationToken);
        }

        record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstAsync(
                r => r.EmployeeId == request.EmployeeId && r.Date == request.Date,
                cancellationToken);

        return Result.Success(ClockInHandler.Map(record));
    }

    private async Task<Result> AuthorizeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole(RoleNames.HRAdmin) ||
            _currentUser.IsInRole(RoleNames.HRManager))
        {
            return Result.Success();
        }

        var managerId = _currentUser.EmployeeId;

        if (managerId is null)
        {
            return Result.Failure("You are not authorized to correct attendance.");
        }

        if (await _managerScope.IsManagerOfAsync(managerId.Value, employeeId, cancellationToken))
        {
            return Result.Success();
        }

        return Result.Failure("You are not authorized to correct attendance for this employee.");
    }
}
