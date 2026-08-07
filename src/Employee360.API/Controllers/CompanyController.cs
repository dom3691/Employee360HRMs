using Employee360.Application.Features.Company;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Company profile configuration (FR-ADM-001).</summary>
[Route("api/v1/company")]
public sealed class CompanyController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetCompanyProfileQuery(), cancellationToken));

    [HttpPut]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCompanyProfileCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpGet("working-calendar")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(WorkingCalendarDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkingCalendar(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetWorkingCalendarQuery(), cancellationToken));

    [HttpPut("working-calendar")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(WorkingCalendarDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWorkingCalendar(
        [FromBody] UpdateWorkingCalendarCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpGet("email-configuration")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(EmailConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailConfiguration(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEmailConfigurationQuery(), cancellationToken));

    [HttpPut("email-configuration")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(EmailConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateEmailConfiguration(
        [FromBody] UpdateEmailConfigurationCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPost("email-configuration/test")]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TestEmailConfiguration(
        [FromBody] TestEmailRequest? body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new TestEmailConfigurationCommand(body?.ToEmail), cancellationToken));

    public sealed record TestEmailRequest(string? ToEmail);
}
