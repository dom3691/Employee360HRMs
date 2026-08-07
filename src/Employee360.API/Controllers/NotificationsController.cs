using Employee360.Application.Common.Models;
using Employee360.Application.Features.Notifications;
using Employee360.Application.Features.SelfService.GetEssDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>In-app notification center endpoints (FR-ESS-001).</summary>
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    /// <summary>Lists the authenticated user's notifications (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
        => FromResult(await Sender.Send(
            new GetMyNotificationsQuery(page, pageSize, unreadOnly), cancellationToken));

    /// <summary>Marks a notification as read (owner only).</summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new MarkNotificationReadCommand(id), cancellationToken));
}
