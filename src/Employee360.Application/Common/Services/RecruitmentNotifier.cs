using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Common.Services;

/// <summary>Notifies recruitment team members (FR-REC-002).</summary>
public interface IRecruitmentNotifier
{
    Task NotifyTeamAsync(string title, string message, CancellationToken cancellationToken = default);
}

public sealed class RecruitmentNotifier : IRecruitmentNotifier
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;

    public RecruitmentNotifier(IApplicationDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task NotifyTeamAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var userIds = await (
            from ur in _context.UserRoles.AsNoTracking()
            join rp in _context.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
            join perm in _context.Permissions.AsNoTracking() on rp.PermissionId equals perm.Id
            where perm.Name == Permissions.Recruitment.Manage
            select ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _notifications.NotifyUserAsync(userId, title, message, cancellationToken: cancellationToken);
        }
    }
}
