using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Roles.RemoveRole;

/// <summary>Removes a role from a user. System Admin only (FR-AUTH-006/007).</summary>
/// <param name="UserId">The target user.</param>
/// <param name="RoleName">The role to remove.</param>
public sealed record RemoveRoleCommand(Guid UserId, string RoleName) : IRequest<Result>;
