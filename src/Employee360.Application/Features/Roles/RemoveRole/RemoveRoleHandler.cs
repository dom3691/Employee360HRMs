using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Roles.RemoveRole;

/// <summary>
/// Handles <see cref="RemoveRoleCommand"/> with an explicit AuditLog entry
/// (join-table changes are not covered by the audit interceptor).
/// </summary>
public sealed class RemoveRoleHandler : IRequestHandler<RemoveRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveRoleHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _context.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(
                ur => ur.UserId == request.UserId && ur.Role.Name == request.RoleName,
                cancellationToken);

        if (assignment is null)
        {
            return Result.Failure($"User does not hold the '{request.RoleName}' role.");
        }

        _context.UserRoles.Remove(assignment);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(UserRole),
            EntityId = $"{assignment.UserId}:{assignment.RoleId}",
            Action = "RoleRemoved",
            OldValues = $$"""{"userId":"{{assignment.UserId}}","userEmail":"{{assignment.User.Email}}","role":"{{assignment.Role.Name}}"}""",
            UserId = _currentUserService.UserId,
            Timestamp = _dateTimeProvider.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
