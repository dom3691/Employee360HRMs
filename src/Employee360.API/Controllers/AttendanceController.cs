using Employee360.Application.Features.Attendance;
using Employee360.Application.Common.Models;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Attendance clock-in/out, device ingestion, and records (FR-ATT-001, FR-ATT-005, FR-ATT-006).</summary>
[Route("api/v1/attendance")]
public sealed class AttendanceController : ApiControllerBase
{
    /// <summary>Clocks the authenticated employee in (FR-ATT-001).</summary>
    [HttpPost("clock-in")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockIn(CancellationToken cancellationToken)
    {
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        return FromResult(await Sender.Send(new ClockInCommand(sourceIp), cancellationToken));
    }

    /// <summary>Clocks the authenticated employee out (FR-ATT-001).</summary>
    [HttpPost("clock-out")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockOut(CancellationToken cancellationToken)
    {
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        return FromResult(await Sender.Send(new ClockOutCommand(sourceIp), cancellationToken));
    }

    /// <summary>Ingests a biometric device clock event (FR-ATT-005).</summary>
    [HttpPost("device-events")]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestDeviceEvent(
        [FromBody] DeviceEventPayload payload,
        [FromHeader(Name = "X-Device-Api-Key")] string? apiKey,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new IngestDeviceEventCommand(payload, apiKey), cancellationToken));

    /// <summary>Lists attendance records with optional filters.</summary>
    [HttpGet("records")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(typeof(PagedResult<AttendanceRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetAttendanceRecordsQuery(page, pageSize, employeeId, fromDate, toDate),
            cancellationToken));

    /// <summary>Manual attendance correction by HR/Manager (FR-ATT-006).</summary>
    [HttpPost("corrections")]
    [HasPermission(Permissions.Attendance.ViewTeam)]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Correct(
        [FromBody] ManualAttendanceCorrectionCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Creates or updates a draft weekly timesheet (FR-ATT-004).</summary>
    [HttpPost("timesheets")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(typeof(TimesheetDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertTimesheet(
        [FromBody] UpsertTimesheetCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Submits a timesheet for approval.</summary>
    [HttpPost("timesheets/{id:guid}/submit")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitTimesheet(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new SubmitTimesheetCommand(id), cancellationToken));

    /// <summary>Approves or rejects a submitted timesheet.</summary>
    [HttpPost("timesheets/{id:guid}/review")]
    [HasPermission(Permissions.Attendance.ViewTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReviewTimesheet(
        [FromRoute] Guid id,
        [FromBody] ReviewTimesheetRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new ReviewTimesheetCommand(id, body.Approve, body.Comments),
            cancellationToken));

    /// <summary>Lists timesheets for an employee.</summary>
    [HttpGet("timesheets")]
    [HasPermission(Permissions.Attendance.ClockInOut)]
    [ProducesResponseType(typeof(PagedResult<TimesheetListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTimesheets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetTimesheetsQuery(page, pageSize, employeeId), cancellationToken));

    /// <summary>Request body for timesheet review.</summary>
    public sealed record ReviewTimesheetRequest(bool Approve, string? Comments);
}
