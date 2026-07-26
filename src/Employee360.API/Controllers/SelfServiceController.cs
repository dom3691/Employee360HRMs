using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.GetEmployeesPaged;
using Employee360.Application.Features.SelfService.GetEssDashboard;
using Employee360.Application.Features.SelfService.GetMssDashboard;
using Employee360.Application.Features.SelfService.GetMyTeam;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>ESS/MSS dashboard endpoints (FR-ESS-001/002, FR-MSS-001/002).</summary>
[Route("api/v1/self-service")]
public sealed class SelfServiceController : ApiControllerBase
{
    /// <summary>Employee dashboard: balances, pending requests, notifications, quick actions.</summary>
    [HttpGet("ess-dashboard")]
    [Authorize]
    [ProducesResponseType(typeof(EssDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EssDashboard(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEssDashboardQuery(), cancellationToken));

    /// <summary>Manager dashboard: pending approvals, team on leave today, headcount, birthdays.</summary>
    [HttpGet("mss-dashboard")]
    [HasPermission(Permissions.Employees.ViewTeam)]
    [ProducesResponseType(typeof(MssDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MssDashboard(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetMssDashboardQuery(), cancellationToken));

    /// <summary>Team list — paged, searchable, status filter; manager-scoped.</summary>
    [HttpGet("my-team")]
    [HasPermission(Permissions.Employees.ViewTeam)]
    [ProducesResponseType(typeof(PagedResult<EmployeeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyTeam(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetMyTeamQuery(page, pageSize, status, search), cancellationToken));
}
