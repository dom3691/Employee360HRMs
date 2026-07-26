using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Roles.AssignRole;

/// <summary>Assigns a role to a user. System Admin only (FR-AUTH-006/007).</summary>
/// <param name="UserId">The target user.</param>
/// <param name="RoleName">The role to assign.</param>
public sealed record AssignRoleCommand(Guid UserId, string RoleName) : IRequest<Result>;
