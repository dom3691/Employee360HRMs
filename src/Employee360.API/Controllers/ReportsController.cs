using Employee360.Application.Features.Reports;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>
/// Reports and analytics endpoints (FR-RPT-001..015 + dashboards).
/// </summary>
[Route("api/v1/reports")]
public sealed class ReportsController : ApiControllerBase
{
    /// <summary>Lists available report definitions.</summary>
    [HttpGet]
    [HasPermission(Permissions.Reports.ViewHR)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
    [ProducesResponseType(typeof(IReadOnlyList<ReportCatalogItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReports(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ListReportsQuery(), cancellationToken));

    /// <summary>
    /// Runs a report by id. Use format=json (default), csv, or xlsx for export.
    /// </summary>
    [HttpGet("{reportId}")]
    [ProducesResponseType(typeof(ReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport(
        [FromRoute] string reportId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? year,
        [FromQuery] string format = ReportFormats.Json,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorizedForReport(reportId))
        {
            return Forbid();
        }

        var result = await Sender.Send(
            new GetReportQuery(reportId, fromDate, toDate, year, format),
            cancellationToken);

        if (result.IsFailure)
        {
            return FromResult(result);
        }

        var response = result.Value!;

        if (response.ExportBytes is not null &&
            response.ContentType is not null &&
            response.FileName is not null)
        {
            return File(response.ExportBytes, response.ContentType, response.FileName);
        }

        return Ok(response);
    }

    private bool IsAuthorizedForReport(string reportId)
    {
        if (reportId.Equals(ReportIds.ExecutiveDashboard, StringComparison.OrdinalIgnoreCase))
        {
            return HasClaimPermission(Permissions.Reports.ViewExecutive);
        }

        return HasClaimPermission(Permissions.Reports.ViewHR);
    }

    private bool HasClaimPermission(string permission) =>
        User.HasClaim("permission", permission);
}
