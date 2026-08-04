using Employee360.Application.Features.Company;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Company profile configuration (FR-ADM-001).</summary>
[Route("api/v1/company")]
public sealed class CompanyController : ApiControllerBase
{
    /// <summary>Gets the organization company profile.</summary>
    [HttpGet]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetCompanyProfileQuery(), cancellationToken));

    /// <summary>Updates the organization company profile.</summary>
    [HttpPut]
    [HasPermission(Permissions.Administration.SystemConfiguration)]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCompanyProfileCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));
}
