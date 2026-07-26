using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Roles.AssignRole;

/// <summary>
/// Handles <see cref="AssignRoleCommand"/>. UserRole is a join row outside the
/// audit interceptor's reach, so an explicit AuditLog entry records the change
/// (permission changes must be audited, PRD Section 7.1).
/// </summary>
public sealed class AssignRoleHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignRoleHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);

        if (role is null)
        {
            return Result.Failure($"Role '{request.RoleName}' not found.");
        }

        var alreadyAssigned = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);

        if (alreadyAssigned)
        {
            return Result.Failure($"User already holds the '{role.Name}' role.");
        }

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(UserRole),
            EntityId = $"{user.Id}:{role.Id}",
            Action = "RoleAssigned",
            NewValues = $$"""{"userId":"{{user.Id}}","userEmail":"{{user.Email}}","role":"{{role.Name}}"}""",
            UserId = _currentUserService.UserId,
            Timestamp = _dateTimeProvider.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
