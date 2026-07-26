using Employee360.Application.Common.Models;
using Employee360.Application.Features.Positions.CreatePosition;
using Employee360.Application.Features.Positions.DeletePosition;
using Employee360.Application.Features.Positions.GetPositions;
using Employee360.Application.Features.Positions.UpdatePosition;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Position / job title management endpoints (FR-EMP-010).</summary>
[Route("api/v1/positions")]
public sealed class PositionsController : ApiControllerBase
{
    /// <summary>Lists positions with department filter, search, and pagination.</summary>
    [HttpGet]
    [HasPermission(Permissions.Positions.View)]
    [ProducesResponseType(typeof(PagedResult<PositionListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetPositionsPagedQuery(page, pageSize, departmentId, search), cancellationToken));

    /// <summary>Gets one position by id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Positions.View)]
    [ProducesResponseType(typeof(PositionListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetPositionByIdQuery(id), cancellationToken));

    /// <summary>Creates a position.</summary>
    [HttpPost]
    [HasPermission(Permissions.Positions.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePositionCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Updates a position.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Positions.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdatePositionRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdatePositionCommand(id, body.Title, body.Code, body.DepartmentId, body.GradeId),
            cancellationToken));

    /// <summary>Soft-deletes a position (blocked while employees hold it).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Positions.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new DeletePositionCommand(id), cancellationToken));

    /// <summary>Request body for <see cref="Update"/> (id from route).</summary>
    public sealed record UpdatePositionRequest(
        string Title,
        string Code,
        Guid? DepartmentId,
        Guid? GradeId);
}
