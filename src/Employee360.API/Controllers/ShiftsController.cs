using Employee360.Application.Common.Models;
using Employee360.Application.Features.Attendance;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Shift management and department assignment (FR-ATT-002).</summary>
[Route("api/v1/shifts")]
public sealed class ShiftsController : ApiControllerBase
{
    /// <summary>Lists shifts (paginated).</summary>
    [HttpGet]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(typeof(PagedResult<ShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetShiftsPagedQuery(page, pageSize), cancellationToken));

    /// <summary>Creates a shift.</summary>
    [HttpPost]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShiftCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Updates a shift.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateShiftRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateShiftCommand(id, body.Name, body.StartTime, body.EndTime, body.GraceMinutes, body.BreakMinutes),
            cancellationToken));

    /// <summary>Soft-deletes a shift.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteShiftCommand(id), cancellationToken));

    /// <summary>Assigns a shift to a department.</summary>
    [HttpPut("departments/{departmentId:guid}/assignment")]
    [HasPermission(Permissions.Attendance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignToDepartment(
        [FromRoute] Guid departmentId,
        [FromBody] AssignShiftRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new AssignShiftToDepartmentCommand(departmentId, body.ShiftId),
            cancellationToken));

    /// <summary>Request body for shift update.</summary>
    public sealed record UpdateShiftRequest(
        string Name,
        TimeOnly StartTime,
        TimeOnly EndTime,
        int GraceMinutes,
        int BreakMinutes);

    /// <summary>Request body for department shift assignment.</summary>
    public sealed record AssignShiftRequest(Guid? ShiftId);
}
