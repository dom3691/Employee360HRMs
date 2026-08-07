using Employee360.Application.Common.Authorization;
using Employee360.Application.Features.Administration.AdminRoles;
using Employee360.Domain.Constants;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Administration endpoints aligned with the frontend contract.</summary>
[Route("api/v1/admin")]
public sealed class AdminController : ApiControllerBase
{
    /// <summary>Lists all roles with UI permission matrix (FR-AUTH-006).</summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.Administration.ManageRoles)]
    [ProducesResponseType(typeof(IReadOnlyList<SystemRoleDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetAdminRolesQuery(), cancellationToken));

    /// <summary>Replaces a role's permission matrix and returns the updated role.</summary>
    [HttpPut("roles/{roleId:guid}/permissions")]
    [HasPermission(Permissions.Administration.ManageRoles)]
    [ProducesResponseType(typeof(SystemRoleDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRolePermissions(
        [FromRoute] Guid roleId,
        [FromBody] UpdateRolePermissionsRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateAdminRolePermissionsCommand(roleId, body.Permissions),
            cancellationToken));

    /// <summary>Request body for matrix-based permission updates.</summary>
    public sealed record UpdateRolePermissionsRequest(
        IReadOnlyDictionary<string, RolePermissionMatrixMapper.ModulePermissionFlags> Permissions);
}
