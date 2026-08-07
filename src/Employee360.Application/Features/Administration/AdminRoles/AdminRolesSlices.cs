using Employee360.Application.Common.Authorization;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Administration.AdminRoles;

public sealed record SystemRoleDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    int UserCount,
    bool IsSystem,
    DateTime LastModified,
    IReadOnlyDictionary<string, RolePermissionMatrixMapper.ModulePermissionFlags> Permissions);

public sealed record GetAdminRolesQuery : IRequest<Result<IReadOnlyList<SystemRoleDefinitionDto>>>;

public sealed class GetAdminRolesHandler : IRequestHandler<GetAdminRolesQuery, Result<IReadOnlyList<SystemRoleDefinitionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminRolesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<SystemRoleDefinitionDto>>> Handle(
        GetAdminRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var items = roles.Select(MapRole).ToList();
        return Result.Success<IReadOnlyList<SystemRoleDefinitionDto>>(items);
    }

    internal static SystemRoleDefinitionDto MapRole(Role role)
    {
        var permissionNames = role.RolePermissions.Select(rp => rp.Permission.Name);
        var lastModified = role.ModifiedAt ?? role.CreatedAt;

        return new SystemRoleDefinitionDto(
            role.Id,
            role.Name,
            role.Description,
            role.UserRoles.Count,
            role.IsSystemRole,
            lastModified,
            RolePermissionMatrixMapper.ToMatrix(permissionNames));
    }
}

public sealed record UpdateAdminRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyDictionary<string, RolePermissionMatrixMapper.ModulePermissionFlags> Permissions)
    : IRequest<Result<SystemRoleDefinitionDto>>;

public sealed class UpdateAdminRolePermissionsValidator : AbstractValidator<UpdateAdminRolePermissionsCommand>
{
    public UpdateAdminRolePermissionsValidator()
    {
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.Permissions).NotNull();
    }
}

public sealed class UpdateAdminRolePermissionsHandler
    : IRequestHandler<UpdateAdminRolePermissionsCommand, Result<SystemRoleDefinitionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateAdminRolePermissionsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SystemRoleDefinitionDto>> Handle(
        UpdateAdminRolePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<SystemRoleDefinitionDto>("Role not found.");
        }

        var requestedNames = RolePermissionMatrixMapper.FromMatrix(request.Permissions);

        var catalog = await _context.Permissions
            .Where(p => requestedNames.Contains(p.Name))
            .ToListAsync(cancellationToken);

        var unknown = requestedNames.Except(catalog.Select(p => p.Name)).ToList();
        if (unknown.Count > 0)
        {
            return Result.Failure<SystemRoleDefinitionDto>(
                $"Unknown permissions: {string.Join(", ", unknown)}.");
        }

        var oldPermissionNames = role.RolePermissions
            .Select(rp => rp.Permission.Name)
            .OrderBy(n => n)
            .ToList();

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

        role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstAsync(r => r.Id == request.RoleId, cancellationToken);

        return Result.Success(GetAdminRolesHandler.MapRole(role));
    }
}
