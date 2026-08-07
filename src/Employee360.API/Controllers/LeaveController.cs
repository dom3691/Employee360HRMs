using Employee360.Application.Common.Models;
using Employee360.Application.Features.Leave.AdjustLeaveBalance;
using Employee360.Application.Features.Leave.ApplyLeave;
using Employee360.Application.Features.Leave.ApproveLeave;
using Employee360.Application.Features.Leave.ConfigureLeavePolicy;
using Employee360.Application.Features.Leave.CreateLeaveType;
using Employee360.Application.Features.Leave.GetLeaveBalances;
using Employee360.Application.Features.Leave.GetLeaveRequests;
using Employee360.Application.Features.Leave.Policies;
using Employee360.Application.Features.Leave.PublicHolidays;
using Employee360.Application.Features.Leave.RejectLeave;
using Employee360.Application.Features.Leave.ReturnLeaveForRevision;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
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

    /// <summary>Org-wide HR leave requests view.</summary>
    [HttpGet("requests")]
    [HasPermission(Permissions.Leave.ApproveAll)]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AllRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? year = null,
        [FromQuery] string? search = null,
        [FromQuery] LeaveRequestStatus? status = null,
        [FromQuery] string? leaveType = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetAllLeaveRequestsQuery(page, pageSize, year, search, status, leaveType),
            cancellationToken));

    /// <summary>Lists the authenticated employee's leave requests.</summary>
    [HttpGet("requests/mine")]
    [HasPermission(Permissions.Leave.Apply)]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetMyLeaveRequestsQuery(page, pageSize, year), cancellationToken));

    /// <summary>Lists pending/escalated requests awaiting the authenticated manager.</summary>
    [HttpGet("requests/approval-queue")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovalQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetApprovalQueueQuery(page, pageSize), cancellationToken));

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
    // Leave policies (frontend contract)
    // -----------------------------------------------------------------------

    [HttpGet("policies")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(typeof(PagedResult<LeavePolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetLeavePoliciesQuery(page, pageSize, search), cancellationToken));

    [HttpPost("policies")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePolicy(
        [FromBody] UpsertLeavePolicyRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new CreateLeavePolicyCommand(body), cancellationToken));

    [HttpPut("policies/{id:guid}")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePolicy(
        [FromRoute] Guid id,
        [FromBody] UpsertLeavePolicyRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new UpdateLeavePolicyCommand(id, body), cancellationToken));

    [HttpDelete("policies/{id:guid}")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePolicy(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteLeavePolicyCommand(id), cancellationToken));

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
        [FromBody] AddHolidayRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new AddPublicHolidayCommand(body.Date, body.Name, body.Type, body.IsRecurring, body.Region),
            cancellationToken));

    /// <summary>Updates a public holiday by id.</summary>
    [HttpPut("holidays/{id:guid}")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateHoliday(
        [FromRoute] Guid id,
        [FromBody] UpdateHolidayRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdatePublicHolidayCommand(id, body.Date, body.Name, body.Type, body.IsRecurring, body.Region),
            cancellationToken));

    /// <summary>Removes a public holiday by id.</summary>
    [HttpDelete("holidays/{id:guid}")]
    [HasPermission(Permissions.Leave.Configure)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteHolidayById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeletePublicHolidayByIdCommand(id), cancellationToken));

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
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "year", "region" })]
    [ProducesResponseType(typeof(IReadOnlyList<PublicHolidayItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Holidays(
        [FromQuery] int? year,
        [FromQuery] string? region,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetPublicHolidaysQuery(year, region), cancellationToken));

    /// <summary>Decision body for approve/reject/return.</summary>
    public sealed record DecisionRequest(string? Comments);

    /// <summary>Policy body for <see cref="ConfigurePolicy"/>.</summary>
    public sealed record ConfigurePolicyRequest(
        decimal AnnualEntitlement,
        AccrualFrequency AccrualFrequency,
        decimal CarryForwardMax,
        int ProbationMonths);

    public sealed record AddHolidayRequest(
        DateOnly Date,
        string Name,
        string? Type = null,
        bool IsRecurring = false,
        string? Region = null);

    public sealed record UpdateHolidayRequest(
        DateOnly Date,
        string Name,
        string? Type,
        bool IsRecurring,
        string? Region);
}
