using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Roles.CreateRole;

/// <summary>Creates a custom (non-system) role. System Admin only (FR-AUTH-006).</summary>
/// <param name="Name">Unique role name.</param>
/// <param name="Description">Role responsibility description.</param>
public sealed record CreateRoleCommand(string Name, string Description) : IRequest<Result<Guid>>;
