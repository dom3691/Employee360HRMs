using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// In-app notification (PRD data model: Notification; FR-ESS-001 recent
/// notifications). Raised by workflow events alongside email.
/// </summary>
public class Notification : AuditableEntity
{
    /// <summary>Recipient user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation to the recipient.</summary>
    public User User { get; set; } = null!;

    /// <summary>Short title, e.g. "Leave Request Approved".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Notification body.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional client route for deep-linking, e.g. "/leave/requests".</summary>
    public string? Link { get; set; }

    /// <summary>True once the user has read the notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>UTC instant the notification was read, or null while unread.</summary>
    public DateTime? ReadAtUtc { get; set; }
}
