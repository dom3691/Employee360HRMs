using Employee360.Application.Features.Leave.AdjustLeaveBalance;
using Employee360.Application.Features.Leave.ApplyLeave;
using Employee360.Application.Features.Leave.ApproveLeave;
using Employee360.Application.Features.Leave.ConfigureLeavePolicy;
using Employee360.Application.Features.Leave.CreateLeaveType;
using Employee360.Application.Features.Leave.GetLeaveBalances;
using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Application.Features.Leave.PublicHolidays;
using Employee360.Application.Features.Leave.RejectLeave;
using Employee360.Application.Features.Leave.ReturnLeaveForRevision;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Leave management endpoints (FR-LV-001..012, Appendix F).</summary>
[Route("api/v1/leave")]
public sealed class LeaveController : ApiControllerBase
{
    // -----------------------------------------------------------------------
    // Requests & workflow
    // -----------------------------------------------------------------------

    /// <summary>Submits a leave request for the authenticated employee.</summary>
    [HttpPost("requests")]
    [HasPermission(Permissions.Leave.Apply)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Apply(
        [FromBody] ApplyLeaveCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Lists the authenticated employee's leave requests.</summary>
    [HttpGet("requests/mine")]
    [HasPermission(Permissions.Leave.Apply)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyRequests(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetMyLeaveRequestsQuery(year), cancellationToken));

    /// <summary>Lists pending/escalated requests awaiting the authenticated manager.</summary>
    [HttpGet("requests/approval-queue")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovalQueue(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetApprovalQueueQuery(), cancellationToken));

    /// <summary>Approves a leave request (approver scope enforced in the handler).</summary>
    [HttpPut("requests/{id:guid}/approve")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] DecisionRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ApproveLeaveCommand(id, body.Comments), cancellationToken));

    /// <summary>Rejects a leave request (comment required).</summary>
    [HttpPut("requests/{id:guid}/reject")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] DecisionRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new RejectLeaveCommand(id, body.Comments ?? ""), cancellationToken));

    /// <summary>Returns a leave request to the employee for revision (comment required).</summary>
    [HttpPut("requests/{id:guid}/return")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Return(
        [FromRoute] Guid id,
        [FromBody] DecisionRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new ReturnLeaveForRevisionCommand(id, body.Comments ?? ""), cancellationToken));

    // -----------------------------------------------------------------------
    // Balances
    // -----------------------------------------------------------------------

    /// <summary>Gets the authenticated employee's balances for a year.</summary>
    [HttpGet("balances/mine")]
    [HasPermission(Permissions.Leave.Apply)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveBalanceItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyBalances(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetLeaveBalancesQuery(null, year ?? DateTime.UtcNow.Year), cancellationToken));

    /// <summary>Gets an employee's balances (HR).</summary>
    [HttpGet("balances/{employeeId:guid}")]
    [HasPermission(Permissions.Leave.AdjustBalances)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveBalanceItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EmployeeBalances(
        [FromRoute] Guid employeeId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetLeaveBalancesQuery(employeeId, year ?? DateTime.UtcNow.Year), cancellationToken));

    /// <summary>Manually adjusts a balance (HR, audited with reason).</summary>
    [HttpPost("balances/adjust")]
    [HasPermission(Permissions.Leave.AdjustBalances)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustBalance(
        [FromBody] AdjustLeaveBalanceCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    // -----------------------------------------------------------------------
    // Calendar
    // -----------------------------------------------------------------------

    /// <summary>Team leave calendar over a date range (manager-scoped).</summary>
    [HttpGet("calendar")]
    [HasPermission(Permissions.Leave.ViewTeamCalendar)]
    [ProducesResponseType(typeof(IReadOnlyList<TeamCalendarEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetTeamLeaveCalendarQuery(from, to), cancellationToken));

    // -----------------------------------------------------------------------
    // Configuration (HR)
    // -----------------------------------------------------------------------

    /// <summary>Creates a leave type.</summary>
    [HttpPost("types")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateType(
        [FromBody] CreateLeaveTypeCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Creates or updates a leave type's policy.</summary>
    [HttpPut("types/{id:guid}/policy")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfigurePolicy(
        [FromRoute] Guid id,
        [FromBody] ConfigurePolicyRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new ConfigureLeavePolicyCommand(
                id, body.AnnualEntitlement, body.AccrualFrequency, body.CarryForwardMax, body.ProbationMonths),
            cancellationToken));

    /// <summary>Adds a public holiday.</summary>
    [HttpPost("holidays")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddHoliday(
        [FromBody] AddPublicHolidayCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Removes a public holiday by date (yyyy-MM-dd).</summary>
    [HttpDelete("holidays/{date}")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveHoliday(
        [FromRoute] DateOnly date,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new RemovePublicHolidayCommand(date), cancellationToken));

    /// <summary>Lists public holidays for a year.</summary>
    [HttpGet("holidays")]
    [HasPermission(Permissions.Leave.Apply)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "year" })]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Holidays(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetPublicHolidaysQuery(year ?? DateTime.UtcNow.Year), cancellationToken));

    /// <summary>Decision body for approve/reject/return.</summary>
    public sealed record DecisionRequest(string? Comments);

    /// <summary>Policy body for <see cref="ConfigurePolicy"/>.</summary>
    public sealed record ConfigurePolicyRequest(
        decimal AnnualEntitlement,
        Employee360.Domain.Enums.AccrualFrequency AccrualFrequency,
        decimal CarryForwardMax,
        int ProbationMonths);
}
