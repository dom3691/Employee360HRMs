using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Default <see cref="INotificationService"/>: persists notification rows
/// immediately. Failures are logged and never break the calling workflow.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyUserAsync(
        Guid userId, string title, string message, string? link = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Link = link,
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}: {Title}", userId, title);
        }
    }

    /// <inheritdoc />
    public async Task NotifyEmployeeAsync(
        Guid employeeId, string title, string message, string? link = null,
        CancellationToken cancellationToken = default)
    {
        var userId = await _context.Users
            .Where(u => u.EmployeeId == employeeId && u.IsActive)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return; // employee has no login account yet
        }

        await NotifyUserAsync(userId.Value, title, message, link, cancellationToken);
    }
}
