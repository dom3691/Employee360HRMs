using Azure;
using Azure.Communication.Email;
using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Employee360.Infrastructure.Services.Email;

/// <summary>
/// Azure Communication Services email sender for production deployments
/// (PRD Integration: Azure Communication Services).
/// </summary>
public sealed class AzureCommunicationEmailService : IEmailSender
{
    private readonly AzureCommunicationEmailSettings _settings;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    public AzureCommunicationEmailService(
        IOptions<AzureCommunicationEmailSettings> settings,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "Email:AzureCommunicationServices:ConnectionString is not configured.");
        }

        var client = new EmailClient(_settings.ConnectionString);

        var message = new EmailMessage(
            senderAddress: _settings.FromAddress,
            content: new EmailContent(subject)
            {
                Html = htmlBody,
            },
            recipients: new EmailRecipients([new EmailAddress(to)]));

        if (!string.IsNullOrEmpty(_settings.FromDisplayName))
        {
            message.Headers["From"] = $"{_settings.FromDisplayName} <{_settings.FromAddress}>";
        }

        var operation = await client.SendAsync(
            WaitUntil.Completed,
            message,
            cancellationToken);

        _logger.LogInformation(
            "ACS email sent to {Recipient}: {Subject} (operation {OperationId})",
            to,
            subject,
            operation.Id);
    }
}
