using Employee360.Application.Features.Employees.ReviewProfileChangeRequest;
using Employee360.Application.Features.Employees.SubmitProfileChangeRequest;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Profile change request workflow endpoints (FR-EMP-011).</summary>
[Route("api/v1/profile-change-requests")]
public sealed class ProfileChangeRequestsController : ApiControllerBase
{
    /// <summary>Submits a change request for the authenticated employee's own profile.</summary>
    [HttpPost]
    [HasPermission(Permissions.Employees.EditOwnProfile)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitProfileChangeRequestCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>HR review: approves (applies changes) or rejects a pending request.</summary>
    [HttpPut("{id:guid}/review")]
    [HasPermission(Permissions.Employees.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Review(
        [FromRoute] Guid id,
        [FromBody] ReviewRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new ReviewProfileChangeRequestCommand(id, body.Approve, body.Comment), cancellationToken));

    /// <summary>Request body for <see cref="Review"/>.</summary>
    public sealed record ReviewRequest(bool Approve, string? Comment);
}
