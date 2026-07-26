using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Roles.UpdateRolePermissions;

/// <summary>
/// Replaces a role's permission set with the provided list. System Admin only
/// (FR-AUTH-006).
/// </summary>
/// <param name="RoleId">The role to update.</param>
/// <param name="PermissionNames">The complete new permission set for the role.</param>
public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> PermissionNames)
    : IRequest<Result>;
