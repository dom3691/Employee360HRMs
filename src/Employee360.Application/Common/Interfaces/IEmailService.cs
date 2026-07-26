namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Outbound email abstraction (SMTP in Infrastructure; PRD Integration: SMTP Email).
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an HTML email.</summary>
    /// <param name="to">Recipient address.</param>
    /// <param name="subject">Subject line.</param>
    /// <param name="htmlBody">HTML body content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
