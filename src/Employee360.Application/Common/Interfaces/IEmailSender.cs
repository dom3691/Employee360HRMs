namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Low-level outbound email transport (SMTP or Azure Communication Services).
/// Application code should use <see cref="IEmailService"/> which queues via Hangfire.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an HTML email immediately.</summary>
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
