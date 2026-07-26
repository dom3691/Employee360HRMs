using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Roles.UpdateRolePermissions;

/// <summary>
/// Handles <see cref="UpdateRolePermissionsCommand"/>: validates every permission
/// name against the catalog, replaces the role's grant set, and writes an explicit
/// AuditLog entry with the before/after permission lists.
/// </summary>
public sealed class UpdateRolePermissionsHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRolePermissionsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure("Role not found.");
        }

        var requestedNames = request.PermissionNames.Distinct().ToList();

        var catalog = await _context.Permissions
            .Where(p => requestedNames.Contains(p.Name))
            .ToListAsync(cancellationToken);

        var unknown = requestedNames
            .Except(catalog.Select(p => p.Name))
            .ToList();

        if (unknown.Count > 0)
        {
            return Result.Failure($"Unknown permissions: {string.Join(", ", unknown)}.");
        }

        var oldPermissionNames = role.RolePermissions
            .Select(rp => rp.Permission.Name)
            .OrderBy(n => n)
            .ToList();

        // Replace the grant set.
        _context.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var permission in catalog)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Role),
            EntityId = role.Id.ToString(),
            Action = "PermissionsUpdated",
            OldValues = $$"""{"role":"{{role.Name}}","permissions":[{{string.Join(",", oldPermissionNames.Select(n => $"\"{n}\""))}}]}""",
            NewValues = $$"""{"role":"{{role.Name}}","permissions":[{{string.Join(",", requestedNames.OrderBy(n => n).Select(n => $"\"{n}\""))}}]}""",
            UserId = _currentUserService.UserId,
            Timestamp = _dateTimeProvider.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
