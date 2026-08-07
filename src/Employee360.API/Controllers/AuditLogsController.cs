using Employee360.Application.Common.Models;
using Employee360.Application.Features.AuditLogs;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Global audit log viewer (FR-ADM-004).</summary>
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController : ApiControllerBase
{
    /// <summary>Queries audit logs with optional filters.</summary>
    [HttpGet]
    [HasPermission(Permissions.Administration.ViewAuditLogs)]
    [ProducesResponseType(typeof(PagedResult<AuditLogListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] string? module = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetAuditLogsPagedQuery(
                page, pageSize, userId, entityName, action, module, outcome, search, fromDate, toDate),
            cancellationToken));

    /// <summary>Exports filtered audit logs as CSV.</summary>
    [HttpGet("export")]
    [HasPermission(Permissions.Administration.ViewAuditLogs)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ExportAuditLogsCsvQuery(userId, entityName, fromDate, toDate),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(result.Value!, "text/csv", fileName);
    }
}
