using Employee360.Application.Features.Roles.AssignRole;
using Employee360.Application.Features.Roles.RemoveRole;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>User role assignment endpoints — System Admin only (FR-AUTH-006/007).</summary>
[Route("api/v1/users/{userId:guid}/roles")]
[HasPermission(Permissions.Administration.ManageRoles)]
public sealed class UserRolesController : ApiControllerBase
{
    /// <summary>Assigns a role to a user (audited).</summary>
    /// <param name="userId">The target user.</param>
    /// <param name="body">The role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Assign(
        [FromRoute] Guid userId,
        [FromBody] AssignRoleRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new AssignRoleCommand(userId, body.RoleName), cancellationToken));

    /// <summary>Removes a role from a user (audited).</summary>
    /// <param name="userId">The target user.</param>
    /// <param name="roleName">The role to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{roleName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Remove(
        [FromRoute] Guid userId,
        [FromRoute] string roleName,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new RemoveRoleCommand(userId, roleName), cancellationToken));

    /// <summary>Request body for <see cref="Assign"/>.</summary>
    /// <param name="RoleName">The role name to assign.</param>
    public sealed record AssignRoleRequest(string RoleName);
}
