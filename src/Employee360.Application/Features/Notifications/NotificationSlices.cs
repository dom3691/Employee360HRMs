using Employee360.Application.Common.Extensions;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Application.Features.SelfService.GetEssDashboard;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Notifications;

// ---------------------------------------------------------------------------
// GetMyNotifications
// ---------------------------------------------------------------------------

/// <summary>Lists the authenticated user's notifications, newest first.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (max 100).</param>
/// <param name="UnreadOnly">True to return only unread notifications.</param>
public sealed record GetMyNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool UnreadOnly = false)
    : IRequest<Result<PagedResult<NotificationSummary>>>;

public sealed class GetMyNotificationsValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Handles <see cref="GetMyNotificationsQuery"/>.</summary>
public sealed class GetMyNotificationsHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationSummary>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<NotificationSummary>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return Result.Failure<PagedResult<NotificationSummary>>("Not authenticated.");
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var projected = query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationSummary(
                n.Id, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt));

        return Result.Success(await projected.ToPagedResultAsync(
            request.Page, request.PageSize, cancellationToken));
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
