using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Services.Email;

/// <summary>
/// Hangfire background job that delivers a single email via <see cref="IEmailSender"/>.
/// </summary>
public sealed class EmailSendJob
{
    private readonly IEmailSender _sender;
    private readonly ILogger<EmailSendJob> _logger;

    public EmailSendJob(IEmailSender sender, ILogger<EmailSendJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>Sends one HTML email (invoked by Hangfire).</summary>
    public async Task ExecuteAsync(string to, string subject, string htmlBody)
    {
        await _sender.SendAsync(to, subject, htmlBody);
        _logger.LogInformation("Background email delivered to {Recipient}: {Subject}", to, subject);
    }
}
