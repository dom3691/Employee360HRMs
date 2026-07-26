using Employee360.Application.Features.Roles.CreateRole;
using Employee360.Application.Features.Roles.UpdateRolePermissions;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Role management endpoints — System Admin only (FR-AUTH-006).</summary>
[Route("api/v1/roles")]
[HasPermission(Permissions.Administration.ManageRoles)]
public sealed class RolesController : ApiControllerBase
{
    /// <summary>Creates a custom role.</summary>
    /// <param name="command">Role name and description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    /// <summary>Replaces a role's permission set (audited with before/after).</summary>
    /// <param name="roleId">The role to update.</param>
    /// <param name="body">The complete new permission list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{roleId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePermissions(
        [FromRoute] Guid roleId,
        [FromBody] UpdateRolePermissionsRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateRolePermissionsCommand(roleId, body.PermissionNames),
            cancellationToken));

    /// <summary>Request body for <see cref="UpdatePermissions"/>.</summary>
    /// <param name="PermissionNames">The complete new permission set for the role.</param>
    public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<string> PermissionNames);
}
