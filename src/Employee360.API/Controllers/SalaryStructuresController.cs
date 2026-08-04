using Employee360.Application.Common.Models;
using Employee360.Application.Features.Payroll.SalaryStructures;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Salary structure management (FR-PAY-001).</summary>
[Route("api/v1/payroll/salary-structures")]
public sealed class SalaryStructuresController : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(PagedResult<SalaryStructureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(new GetSalaryStructuresPagedQuery(page, pageSize), cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalaryStructureCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSalaryStructureRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateSalaryStructureCommand(id, body.Name, body.Basic, body.Housing, body.Transport, body.OtherAllowances, body.IsActive),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Payroll.ManageSalaryStructures)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeleteSalaryStructureCommand(id), cancellationToken));

    public sealed record UpdateSalaryStructureRequest(
        string Name, decimal Basic, decimal Housing, decimal Transport, decimal OtherAllowances, bool IsActive);
}
