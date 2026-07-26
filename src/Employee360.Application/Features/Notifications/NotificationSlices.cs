using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.SelfService.GetEssDashboard;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Notifications;

// ---------------------------------------------------------------------------
// GetMyNotifications
// ---------------------------------------------------------------------------

/// <summary>Lists the authenticated user's notifications, newest first.</summary>
/// <param name="UnreadOnly">True to return only unread notifications.</param>
/// <param name="Take">Maximum rows (default 20, max 100).</param>
public sealed record GetMyNotificationsQuery(bool UnreadOnly = false, int Take = 20)
    : IRequest<Result<IReadOnlyList<NotificationSummary>>>;

/// <summary>Handles <see cref="GetMyNotificationsQuery"/>.</summary>
public sealed class GetMyNotificationsHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<IReadOnlyList<NotificationSummary>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<NotificationSummary>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return Result.Failure<IReadOnlyList<NotificationSummary>>("Not authenticated.");
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var take = Math.Clamp(request.Take, 1, 100);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationSummary(
                n.Id, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NotificationSummary>>(items);
    }
}

// ---------------------------------------------------------------------------
// MarkNotificationRead
// ---------------------------------------------------------------------------

/// <summary>Marks one of the authenticated user's notifications as read.</summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

/// <summary>Handles <see cref="MarkNotificationReadCommand"/> with ownership enforcement.</summary>
public sealed class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkNotificationReadHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return Result.Failure("Not authenticated.");
        }

        // Ownership is part of the predicate: other users' notifications are invisible.
        var notification = await _context.Notifications.FirstOrDefaultAsync(
            n => n.Id == request.NotificationId && n.UserId == userId.Value,
            cancellationToken);

        if (notification is null)
        {
            return Result.Failure("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = _dateTimeProvider.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
